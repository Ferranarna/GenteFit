using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using GenteFit.Web.Helpers;
using GenteFit.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

// Salas del centro: solo las gestiona el encargado (el administrador hereda sus permisos).
[Authorize(Roles = "Encargado,Administrador")]
public class SalasController : Controller
{
    private readonly GenteFitContext _db;

    public SalasController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(string? buscar)
    {
        var modelo = new SalasViewModel { Buscar = buscar };

        try
        {
            var repo = new SalaRepositorio(_db);
            modelo.Salas = string.IsNullOrWhiteSpace(buscar)
                ? repo.ObtenerTodos().OrderBy(s => s.NombreSala).ToList()
                : repo.Buscar(buscar.Trim());
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }

    [HttpGet]
    public IActionResult Crear() => View("Formulario", new SalaFormularioViewModel());

    [HttpGet]
    public IActionResult Editar(Guid salaId)
    {
        try
        {
            var s = new SalaRepositorio(_db).ObtenerPorId(salaId);
            if (s == null)
            {
                TempData["Error"] = "La sala no existe.";
                return RedirectToAction(nameof(Index));
            }

            return View("Formulario", new SalaFormularioViewModel
            {
                SalaId = s.SalaId,
                Nombre = s.NombreSala,
                Ubicacion = s.Ubicacion
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar(SalaFormularioViewModel modelo)
    {
        // Si falta algún dato obligatorio, se vuelve al formulario con los mensajes en rojo
        if (!ModelState.IsValid)
            return View("Formulario", modelo);

        var nombre = modelo.Nombre!.Trim();

        try
        {
            var repo = new SalaRepositorio(_db);

            if (modelo.EsNueva)
            {
                repo.Crear(new Sala { NombreSala = nombre, Ubicacion = modelo.Ubicacion!.Value });
                TempData["Ok"] = $"Sala «{nombre}» creada.";
            }
            else
            {
                var sala = repo.ObtenerPorId(modelo.SalaId!.Value);
                if (sala == null)
                {
                    TempData["Error"] = "La sala no existe.";
                    return RedirectToAction(nameof(Index));
                }

                sala.NombreSala = nombre;
                sala.Ubicacion = modelo.Ubicacion!.Value;
                repo.Actualizar(sala);
                TempData["Ok"] = $"Sala «{nombre}» modificada.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidacionException ex)
        {
            // Por ejemplo, "Ya existe una sala con ese nombre."
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View("Formulario", modelo);
    }

    // Informe de uso de salas. Por defecto, el mes actual.
    [HttpGet]
    public IActionResult Informe(DateTime? desde, DateTime? hasta)
    {
        var hoy = DateTime.Today;
        var primeroDeMes = new DateTime(hoy.Year, hoy.Month, 1);
        var modelo = new InformeSalasViewModel
        {
            Desde = (desde ?? primeroDeMes).Date,
            Hasta = (hasta ?? primeroDeMes.AddMonths(1).AddDays(-1)).Date
        };

        try
        {
            modelo.Informe = new SalaRepositorio(_db).Informe(modelo.Desde, modelo.Hasta);
        }
        catch (ValidacionException ex)
        {
            ViewBag.Error = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }
}
