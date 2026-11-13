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

// Una sola pantalla para todos los roles: el encargado la ve con gestión y el resto solo consulta.
[Authorize(Roles = "Cliente,Recepcionista,Encargado,Administrador")]
public class ActividadesController : Controller
{
    private readonly GenteFitContext _db;

    public ActividadesController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(string? buscar, string? orden)
    {
        var modelo = new ActividadesViewModel
        {
            Buscar = buscar,
            Orden = orden == "intensidad" ? "intensidad" : "nombre",
            PuedeEditar = User.IsInRole(nameof(Rol.Encargado))
        };

        try
        {
            var repo = new ActividadRepositorio(_db);
            var actividades = string.IsNullOrWhiteSpace(buscar)
                ? repo.ObtenerTodos()
                : repo.Buscar(buscar.Trim());

            // Se ordena en memoria: la intensidad se guarda como texto en la BD y allí saldría por orden alfabético
            modelo.Actividades = modelo.Orden == "intensidad"
                ? actividades.OrderBy(a => a.Intensidad).ThenBy(a => a.NombreActividad).ToList()
                : actividades.OrderBy(a => a.NombreActividad).ToList();
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }
    [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Crear() => View("Formulario", new ActividadFormularioViewModel());

    [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Editar(Guid actividadId)
    {
        try
        {
            var a = new ActividadRepositorio(_db).ObtenerPorId(actividadId);
            if (a == null)
            {
                TempData["Error"] = "La actividad no existe.";
                return RedirectToAction(nameof(Index));
            }

            return View("Formulario", new ActividadFormularioViewModel
            {
                ActividadId = a.ActividadId,
                Nombre = a.NombreActividad,
                Especialidad = a.Especialidad,
                Descripcion = a.Descripcion,
                Intensidad = a.Intensidad,
                LimiteUsuarios = a.LimiteUsuarios
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Guardar(ActividadFormularioViewModel modelo)
    {
        // Si falta algún dato obligatorio, se vuelve al formulario con los mensajes en rojo
        if (!ModelState.IsValid)
            return View("Formulario", modelo);

        try
        {
            var repo = new ActividadRepositorio(_db);

            if (modelo.EsNueva)
            {
                repo.Crear(new Actividad
                {
                    NombreActividad = modelo.Nombre!.Trim(),
                    Especialidad = modelo.Especialidad!.Value,
                    Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim(),
                    Intensidad = modelo.Intensidad!.Value,
                    LimiteUsuarios = modelo.LimiteUsuarios
                });
                TempData["Ok"] = $"Actividad «{modelo.Nombre!.Trim()}» creada.";
            }
            else
            {
                // Se carga la actividad de la BD y solo se cambian los campos del formulario (la imagen se conserva)
                var actividad = repo.ObtenerPorId(modelo.ActividadId!.Value);
                if (actividad == null)
                {
                    TempData["Error"] = "La actividad no existe.";
                    return RedirectToAction(nameof(Index));
                }

                actividad.NombreActividad = modelo.Nombre!.Trim();
                actividad.Especialidad = modelo.Especialidad!.Value;
                actividad.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim();
                actividad.Intensidad = modelo.Intensidad!.Value;
                actividad.LimiteUsuarios = modelo.LimiteUsuarios;
                repo.Actualizar(actividad);
                TempData["Ok"] = $"Actividad «{actividad.NombreActividad}» modificada.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidacionException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View("Formulario", modelo);
    }
    // Informe de actividades más reservadas. Por defecto, el mes actual.
    [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Informe(DateTime? desde, DateTime? hasta)
    {
        var hoy = DateTime.Today;
        var modelo = new InformeActividadesViewModel
        {
            Desde = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date,
            Hasta = (hasta ?? new DateTime(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1)).Date
        };

        try
        {
            modelo.Informe = new ActividadRepositorio(_db).Informe(modelo.Desde, modelo.Hasta);
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
