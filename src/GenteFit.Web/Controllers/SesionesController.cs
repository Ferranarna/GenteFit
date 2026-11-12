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

// Es la misma para todos los roles; solo el cliente ve los botones para reservar.
[Authorize(Roles = "Cliente,Recepcionista,Encargado,Administrador")]
public class SesionesController : Controller
{
    private readonly GenteFitContext _db;

    public SesionesController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(DateTime? dia, Guid? actividad)
    {
        var modelo = new SesionesViewModel
        {
            Dia = (dia ?? DateTime.Today).Date,
            ActividadId = actividad,
            PuedeReservar = User.IsInRole(nameof(Rol.Cliente)),
            PuedeEditar = User.IsInRole(nameof(Rol.Encargado))
        };

        try
        {
            var sesiones = new SesionRepositorio(_db);
            modelo.Actividades = new ActividadRepositorio(_db).ObtenerTodos()
                .OrderBy(a => a.NombreActividad).ToList();
            modelo.DiasConSesiones = sesiones.DiasConSesiones(modelo.Dia.Year, modelo.Dia.Month, actividad);
            modelo.Sesiones = sesiones.ListarDelDia(modelo.Dia, actividad,
                modelo.PuedeReservar ? UsuarioActual() : null);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(Rol.Cliente))]
    public IActionResult Reservar(Guid sesionId, DateTime dia, Guid? actividad)
    {
        try
        {
            var reservas = new ReservaRepositorio(_db);
            var reserva = reservas.Reservar(UsuarioActual(), sesionId);
            TempData["Ok"] = reserva.Estado == EstadoReserva.Confirmada
                ? "Reserva confirmada. La tienes en Mis reservas."
                : $"La sesión está completa: estás en la lista de espera en la posición {reservas.ObtenerPosicion(reserva.ReservaId)}.";
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }

        return RedirectToAction(nameof(Index), new { dia = dia.ToString("yyyy-MM-dd"), actividad });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Cancelar(Guid sesionId, DateTime dia, Guid? actividad)
    {
        try
        {
            var afectados = new SesionRepositorio(_db).Cancelar(sesionId, DateTime.Now);
            TempData["Ok"] = afectados.Count == 0
                ? "Sesión cancelada. No tenía reservas."
                : $"Sesión cancelada. Se han anulado {afectados.Count} reservas.";
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }

        return RedirectToAction(nameof(Index), new { dia = dia.ToString("yyyy-MM-dd"), actividad });
    }

    [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Reservas(Guid sesionId, DateTime dia, Guid? actividad)
    {
        try
        {
            var sesion = new SesionRepositorio(_db).ObtenerConDetalle(sesionId);
            if (sesion == null)
            {
                TempData["Error"] = "La sesión no existe.";
                return RedirectToAction(nameof(Index), new { dia = dia.ToString("yyyy-MM-dd"), actividad });
            }

            var reservas = new ReservaRepositorio(_db);
            return View(new ReservasDeSesionViewModel
            {
                Sesion = sesion,
                Confirmadas = reservas.ListarConfirmados(sesionId),
                EnEspera = reservas.ListarListaEspera(sesionId),
                Dia = dia,
                ActividadId = actividad
            });


        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction(nameof(Index), new { dia = dia.ToString("yyyy-MM-dd"), actividad });
        }
    }

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

  [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Crear(DateTime? dia) =>
        MostrarFormulario(new SesionFormularioViewModel { Fecha = dia ?? DateTime.Today });

    [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Editar(Guid sesionId, DateTime dia, Guid? actividad)
    {
        try
        {
            var s = new SesionRepositorio(_db).ObtenerConDetalle(sesionId);
            if (s == null)
            {
                TempData["Error"] = "La sesión no existe.";
                return RedirectToAction(nameof(Index), new { dia = dia.ToString("yyyy-MM-dd"), actividad });
            }

            return MostrarFormulario(new SesionFormularioViewModel
            {
                SesionId = s.SesionId,
                ActividadId = s.ActividadId,
                SalaId = s.SalaId,
                EntrenadorId = s.EntrenadorId,
                Fecha = s.Fecha.Date,
                Hora = s.Fecha.TimeOfDay,
                Duracion = s.Duracion
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction(nameof(Index), new { dia = dia.ToString("yyyy-MM-dd"), actividad });
        }
    }

    private IActionResult MostrarFormulario(SesionFormularioViewModel modelo)
    {
        try
        {
            modelo.Actividades = new ActividadRepositorio(_db).ObtenerTodos()
                .OrderBy(a => a.NombreActividad).ToList();
            modelo.Salas = new SalaRepositorio(_db).ObtenerTodos()
                .OrderBy(s => s.NombreSala).ToList();
            modelo.Entrenadores = new EntrenadorRepositorio(_db).ObtenerActivos();
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View("Formulario", modelo);
    }
}