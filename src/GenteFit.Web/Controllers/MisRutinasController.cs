using System.Security.Claims;
using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Enums;
using GenteFit.Web.Helpers;
using GenteFit.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

// Rutinas de un cliente. El cliente ve todas las suyas (modifica las que se hizo él); un entrenador que lo tenga
// como cliente suyo (con una cita programada) ve y modifica solo las que él le ha creado.
// Todas las acciones comprueban de quién es la rutina.

[Authorize(Roles = "Cliente,Entrenador")]
public class MisRutinasController : Controller
{
    private const string NoExiste = "La rutina no existe.";

    // Valores con los que entra un ejercicio nuevo; se cambian en la fila de añadir
    private const int SeriesPorDefecto = 3;
    private const int RepeticionesPorDefecto = 10;

    private readonly GenteFitContext _db;

    public MisRutinasController(GenteFitContext db)
    {
        _db = db;
    }

    // ---------- Lista de rutinas (cada tarjeta despliega su editor) ----------

    [HttpGet]
    public IActionResult Index(Guid? abierta, bool nueva, Guid? clienteId)
    {
        var modelo = new MisRutinasViewModel { Abierta = abierta, Nueva = nueva };
        try
        {
            var propietario = Propietario(clienteId);
            var rutinas = new RutinaRepositorio(_db);
            if (EsEntrenador)
            {
                modelo.Cliente = new UsuarioRepositorio(_db).ObtenerPorId(propietario);
                modelo.Rutinas = rutinas.ObtenerDeEntrenador(UsuarioActual(), propietario);
            }
            else
            {
                modelo.Rutinas = rutinas.ObtenerDeCliente(propietario);
            }
            modelo.Catalogo = new EjercicioRepositorio(_db).ObtenerTodos().OrderBy(e => e.NombreEjercicio).ToList();
        }
        catch (ValidacionException)
        {
            // Un entrenador que llega sin que el cliente sea suyo: de vuelta a su lista de clientes
            return RedirectToAction("Index", "MisClientes");
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Crear(string? nombre, Guid? clienteId)
    {
        try
        {
            var propietario = Propietario(clienteId);
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0)
                throw new ValidacionException("El nombre es obligatorio.");
            if (nombre.Length > 100)
                throw new ValidacionException("El nombre no puede tener más de 100 caracteres.");

            // Un entrenador la crea para su cliente (el repositorio comprueba que lo es); un cliente, para sí mismo.
            // La rutina guarda quién la creó y la fecha.
            var repo = new RutinaRepositorio(_db);
            var rutina = EsEntrenador
                ? repo.CrearParaCliente(UsuarioActual(), propietario, nombre)
                : repo.Crear(propietario, nombre);

            TempData["Ok"] = "Rutina creada. Añádele ejercicios en la última fila.";
            return RedirectToAction(nameof(Index), new { abierta = rutina.RutinaId, clienteId });
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }
        return RedirectToAction(nameof(Index), new { nueva = true, clienteId });
    }

    // El cliente elimina cualquiera de las suyas (también las que le hizo su entrenador);
    // un entrenador, solo las que él creó para ese cliente
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Eliminar(Guid rutinaId, Guid? clienteId)
    {
        try
        {
            var propietario = Propietario(clienteId);
            var repo = new RutinaRepositorio(_db);
            var puede = EsEntrenador
                ? repo.EsDeCliente(rutinaId, propietario, UsuarioActual())
                : repo.PerteneceACliente(rutinaId, propietario);
            if (!puede)
                throw new ValidacionException(NoExiste);

            repo.Eliminar(rutinaId);   // sus ejercicios se borran con ella (composición)
            TempData["Ok"] = "Rutina eliminada.";
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }
        return RedirectToAction(nameof(Index), new { clienteId });
    }

    // ---------- Editar una rutina ----------

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GuardarSeries(Guid rutinaId, Guid? clienteId, string? nombre, List<FilaSeries>? filas,
        Guid? ejercicioId, int series = SeriesPorDefecto, int repeticiones = RepeticionesPorDefecto)
    {
        filas ??= [];
        return EnRutina(rutinaId, clienteId, (repo, propietario) =>
        {
            // Primero se comprueba todo y después se guarda, para no dejar la rutina a medias
            foreach (var f in filas)
                RutinaRepositorio.ValidarConfiguracion(f.Series, f.Repeticiones);
            if (ejercicioId != null)
                RutinaRepositorio.ValidarConfiguracion(series, repeticiones);
            if (filas.Any(f => !repo.DetalleEsDeCliente(f.DetalleId, propietario, Creador)))
                throw new ValidacionException("El ejercicio no está en la rutina.");

            repo.Renombrar(rutinaId, (nombre ?? "").Trim());
            foreach (var f in filas)
                repo.ConfigurarEjercicio(f.DetalleId, f.Series, f.Repeticiones);

            // Las filas llegan en el orden en que se dejaron (arrastrando)
            repo.Ordenar(filas.Select(f => f.DetalleId).ToList());

            // Si en la última fila hay un ejercicio elegido, también se añade (al final)
            if (ejercicioId == null)
                return "Cambios guardados.";

            repo.AnadirEjercicio(rutinaId, ejercicioId.Value, repo.SiguienteOrden(rutinaId), series, repeticiones);
            return "Cambios guardados y ejercicio añadido.";
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Anadir(Guid rutinaId, Guid? clienteId, Guid? ejercicioId,
        int series = SeriesPorDefecto, int repeticiones = RepeticionesPorDefecto)
    {
        return EnRutina(rutinaId, clienteId, (repo, _) =>
        {
            if (ejercicioId == null)
                throw new ValidacionException("Elige el ejercicio que quieres añadir.");

            repo.AnadirEjercicio(rutinaId, ejercicioId.Value, repo.SiguienteOrden(rutinaId), series, repeticiones);
            return "Ejercicio añadido.";
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Quitar(Guid rutinaId, Guid? clienteId, Guid? detalleId)
    {
        return EnRutina(rutinaId, clienteId, (repo, propietario) =>
        {
            if (detalleId == null || !repo.DetalleEsDeCliente(detalleId.Value, propietario, Creador))
                throw new ValidacionException("Elige el ejercicio que quieres quitar.");

            repo.QuitarEjercicio(detalleId.Value);
            return "Ejercicio quitado de la rutina.";
        });
    }

    // ---------- "Añadir a rutina" desde el catálogo de Ejercicios (solo clientes, y solo a sus propias rutinas) ----------

    [HttpGet, Authorize(Roles = nameof(Rol.Cliente))]
    public IActionResult AnadirDesdeCatalogo(Guid ejercicioId)
    {
        try
        {
            var ejercicio = new EjercicioRepositorio(_db).ObtenerPorId(ejercicioId);
            if (ejercicio == null)
            {
                TempData["Error"] = "El ejercicio no existe.";
                return RedirectToAction("Index", "Ejercicios");
            }

            return View(new AnadirARutinaViewModel
            {
                Ejercicio = ejercicio,
                // Las que le ha hecho su entrenador no se modifican, así que no se ofrecen
                Rutinas = new RutinaRepositorio(_db).ObtenerDeCliente(UsuarioActual())
                    .Where(r => r.EntrenadorId == null).ToList()
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction("Index", "Ejercicios");
        }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(Rol.Cliente))]
    public IActionResult AnadirDesdeCatalogo(Guid ejercicioId, Guid rutinaId)
    {
        // Mismo proceso que "Añadir" en el editor; al acabar se abre la rutina
        return Anadir(rutinaId, null, ejercicioId, SeriesPorDefecto, RepeticionesPorDefecto);
    }

    // ---------- Ayudas ----------

    private bool EsEntrenador => User.IsInRole(nameof(Rol.Entrenador));

    // Quién creó las rutinas que esta persona puede modificar: un entrenador, las suyas; un cliente, las que se hizo él (null)
    private Guid? Creador => EsEntrenador ? UsuarioActual() : null;

    // De quién son las rutinas que se tocan: las del propio cliente o, si es un entrenador, las de un cliente suyo.
    // Un cliente nunca puede tocar las de otro, aunque cambie el clienteId en la dirección.
    private Guid Propietario(Guid? clienteId)
    {
        if (!EsEntrenador)
            return UsuarioActual();

        if (clienteId == null || !new CitaRepositorio(_db).EsClienteDe(UsuarioActual(), clienteId.Value))
            throw new ValidacionException("El cliente no existe.");
        return clienteId.Value;
    }

    // Comprueba que la rutina se puede modificar, ejecuta la operación, y vuelve a la lista con esa rutina abierta
    private IActionResult EnRutina(Guid rutinaId, Guid? clienteId, Func<RutinaRepositorio, Guid, string> operacion)
    {
        try
        {
            var propietario = Propietario(clienteId);
            var repo = new RutinaRepositorio(_db);
            if (!repo.EsDeCliente(rutinaId, propietario, Creador))
            {
                TempData["Error"] = NoExiste;
                return RedirectToAction(nameof(Index), new { clienteId });
            }

            TempData["Ok"] = operacion(repo, propietario);
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }
        return RedirectToAction(nameof(Index), new { abierta = rutinaId, clienteId });
    }

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}