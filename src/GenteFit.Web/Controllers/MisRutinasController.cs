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

    // Valores con los que entra un ejercicio nuevo; el cliente los cambia después en la tabla
    private const int SeriesPorDefecto = 3;
    private const int RepeticionesPorDefecto = 10;

    private readonly GenteFitContext _db;

    public MisRutinasController(GenteFitContext db)
    {
        _db = db;
    }

    // ---------- Lista de rutinas ----------

    [HttpGet]
    public IActionResult Index()
    {
        var modelo = new MisRutinasViewModel();
        try
        {
            modelo.Rutinas = new RutinaRepositorio(_db).ObtenerDeCliente(UsuarioActual());
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View(modelo);
    }

    [HttpGet]
    public IActionResult Crear() => View(new NuevaRutinaViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Crear(NuevaRutinaViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            var rutina = new RutinaRepositorio(_db).Crear(UsuarioActual(), modelo.Nombre!.Trim());
            TempData["Ok"] = "Rutina creada. Ahora añádele ejercicios.";
            return RedirectToAction(nameof(Detalle), new { rutinaId = rutina.RutinaId });
        }
        catch (ValidacionException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View(modelo);
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

    // ---------- Detalle de una rutina ----------

    [HttpGet]
    public IActionResult Detalle(Guid rutinaId)
    {
        try
        {
            var rutina = new RutinaRepositorio(_db).ObtenerDeCliente(rutinaId, UsuarioActual());
            if (rutina == null)
            {
                TempData["Error"] = NoExiste;
                return RedirectToAction(nameof(Index));
            }

            return View(new RutinaDetalleViewModel
            {
                Rutina = rutina,
                Catalogo = new EjercicioRepositorio(_db).ObtenerTodos().OrderBy(e => e.NombreEjercicio).ToList()
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Renombrar(Guid rutinaId, string? nombre)
    {
        return EnRutina(rutinaId, repo =>
        {
            repo.Renombrar(rutinaId, (nombre ?? "").Trim());
            return "Nombre guardado.";
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GuardarSeries(Guid rutinaId, List<FilaSeries> filas)
    {
        return EnRutina(rutinaId, repo =>
        {
            // Primero se comprueba todo y después se guarda, para no dejar la rutina a medias
            if (filas.Any(f => f.Series <= 0 || f.Repeticiones <= 0))
                throw new ValidacionException("Las series y las repeticiones deben ser mayores que 0.");
            if (filas.Any(f => !repo.DetalleEsDeCliente(f.DetalleId, UsuarioActual())))
                throw new ValidacionException("El ejercicio no está en la rutina.");

            foreach (var f in filas)
                repo.ConfigurarEjercicio(f.DetalleId, f.Series, f.Repeticiones);
            return "Cambios guardados.";
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Anadir(Guid rutinaId, Guid? ejercicioId)
    {
        return EnRutina(rutinaId, repo =>
        {
            if (ejercicioId == null)
                throw new ValidacionException("Elige el ejercicio que quieres añadir.");

            repo.AnadirEjercicio(rutinaId, ejercicioId.Value, repo.SiguienteOrden(rutinaId),
                SeriesPorDefecto, RepeticionesPorDefecto);
            return $"Ejercicio añadido con {SeriesPorDefecto} series de {RepeticionesPorDefecto}. Puedes cambiarlas en la tabla.";
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
        // Mismo proceso que "Añadir" en el detalle; al acabar se abre la rutina
        return Anadir(rutinaId, ejercicioId);
    }

    // ---------- Ayudas ----------

    // Comprueba que la rutina es del cliente, ejecuta la operación, y vuelve al detalle con el aviso
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
        return RedirectToAction(nameof(Detalle), new { rutinaId });
    }

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
