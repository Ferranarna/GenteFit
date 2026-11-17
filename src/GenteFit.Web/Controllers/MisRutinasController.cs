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

// Rutinas del cliente que ha iniciado sesión. Todas las acciones comprueban que la rutina es suya

[Authorize(Roles = nameof(Rol.Cliente))]
public class MisRutinasController : Controller
{
    private const string NoExiste = "La rutina no existe.";

    // Valores con los que entra un ejercicio nuevo; el cliente los cambia en la fila de añadir
    private const int SeriesPorDefecto = 3;
    private const int RepeticionesPorDefecto = 10;

    private readonly GenteFitContext _db;

    public MisRutinasController(GenteFitContext db)
    {
        _db = db;
    }

    // ---------- Lista de rutinas (cada tarjeta despliega su editor) ----------

    [HttpGet]
    public IActionResult Index(Guid? abierta, bool nueva)
    {
        var modelo = new MisRutinasViewModel { Abierta = abierta, Nueva = nueva };
        try
        {
            modelo.Rutinas = new RutinaRepositorio(_db).ObtenerDeCliente(UsuarioActual());
            modelo.Catalogo = new EjercicioRepositorio(_db).ObtenerTodos().OrderBy(e => e.NombreEjercicio).ToList();
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Crear(string? nombre)
    {
        try
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0)
                throw new ValidacionException("El nombre es obligatorio.");
            if (nombre.Length > 100)
                throw new ValidacionException("El nombre no puede tener más de 100 caracteres.");

            var rutina = new RutinaRepositorio(_db).Crear(UsuarioActual(), nombre);
            TempData["Ok"] = "Rutina creada. Añádele ejercicios en la última fila.";
            return RedirectToAction(nameof(Index), new { abierta = rutina.RutinaId });
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }
        return RedirectToAction(nameof(Index), new { nueva = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Eliminar(Guid rutinaId)
    {
        try
        {
            var repo = new RutinaRepositorio(_db);
            if (!repo.EsDeCliente(rutinaId, UsuarioActual()))
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
        return RedirectToAction(nameof(Index));
    }

    // ---------- Editar una rutina ----------

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GuardarSeries(Guid rutinaId, string? nombre, List<FilaSeries>? filas,
    Guid? ejercicioId, int series = SeriesPorDefecto, int repeticiones = RepeticionesPorDefecto)
    {
        filas ??= [];
        return EnRutina(rutinaId, repo =>
        {
            // Primero se comprueba todo y después se guarda, para no dejar la rutina a medias
            foreach (var f in filas)
                RutinaRepositorio.ValidarConfiguracion(f.Series, f.Repeticiones);
            if (ejercicioId != null)
                RutinaRepositorio.ValidarConfiguracion(series, repeticiones);
            if (filas.Any(f => !repo.DetalleEsDeCliente(f.DetalleId, UsuarioActual())))
                throw new ValidacionException("El ejercicio no está en la rutina.");

            repo.Renombrar(rutinaId, (nombre ?? "").Trim());
            foreach (var f in filas)
                repo.ConfigurarEjercicio(f.DetalleId, f.Series, f.Repeticiones);

            // Las filas llegan en el orden en que el cliente las dejó (arrastrando)
            repo.Ordenar(filas.Select(f => f.DetalleId).ToList());

            // Si en la última fila hay un ejercicio elegido, también se añade (al final)
            if (ejercicioId == null)
                return "Cambios guardados.";

            repo.AnadirEjercicio(rutinaId, ejercicioId.Value, repo.SiguienteOrden(rutinaId), series, repeticiones);
            return "Cambios guardados y ejercicio añadido.";
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Anadir(Guid rutinaId, Guid? ejercicioId,
        int series = SeriesPorDefecto, int repeticiones = RepeticionesPorDefecto)
    {
        return EnRutina(rutinaId, repo =>
        {
            if (ejercicioId == null)
                throw new ValidacionException("Elige el ejercicio que quieres añadir.");

            repo.AnadirEjercicio(rutinaId, ejercicioId.Value, repo.SiguienteOrden(rutinaId), series, repeticiones);
            return "Ejercicio añadido.";
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Quitar(Guid rutinaId, Guid? detalleId)
    {
        return EnRutina(rutinaId, repo =>
        {
            if (detalleId == null || !repo.DetalleEsDeCliente(detalleId.Value, UsuarioActual()))
                throw new ValidacionException("Elige el ejercicio que quieres quitar.");

            repo.QuitarEjercicio(detalleId.Value);
            return "Ejercicio quitado de la rutina.";
        });
    }

    // ---------- "Añadir a rutina" desde el catálogo de Ejercicios ----------

    [HttpGet]
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
                Rutinas = new RutinaRepositorio(_db).ObtenerDeCliente(UsuarioActual())
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction("Index", "Ejercicios");
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AnadirDesdeCatalogo(Guid ejercicioId, Guid rutinaId)
    {
        // Mismo proceso que "Añadir" en el editor; al acabar se abre la rutina
        return Anadir(rutinaId, ejercicioId, SeriesPorDefecto, RepeticionesPorDefecto);
    }

    // ---------- Ayudas ----------

    // Comprueba que la rutina es del cliente, ejecuta la operación, y vuelve a la lista con esa rutina abierta
    private IActionResult EnRutina(Guid rutinaId, Func<RutinaRepositorio, string> operacion)
    {
        try
        {
            var repo = new RutinaRepositorio(_db);
            if (!repo.EsDeCliente(rutinaId, UsuarioActual()))
            {
                TempData["Error"] = NoExiste;
                return RedirectToAction(nameof(Index));
            }

            TempData["Ok"] = operacion(repo);
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }
        return RedirectToAction(nameof(Index), new { abierta = rutinaId });
    }

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}