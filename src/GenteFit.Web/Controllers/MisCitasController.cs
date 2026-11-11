using System.Globalization;
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

// El cliente reserva y anula citas con un entrenador personal; el entrenador ve y anula las suyas.
[Authorize(Roles = "Cliente,Entrenador")]
public class MisCitasController : Controller
{
    private static readonly CultureInfo Es = new("es-ES");
    private readonly GenteFitContext _db;

    public MisCitasController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(Guid? entrenador, DateTime? dia, EstadoCita? estado) =>
        User.IsInRole(nameof(Rol.Entrenador)) ? DelEntrenador(estado) : DelCliente(entrenador, dia);

    private IActionResult DelCliente(Guid? entrenador, DateTime? dia)
    {
        var modelo = new MisCitasViewModel { EntrenadorId = entrenador, Dia = (dia ?? DateTime.Today).Date };
        try
        {
            var citas = new CitaRepositorio(_db);
            modelo.Entrenadores = new EntrenadorRepositorio(_db)
                .ObtenerActivosPorEspecialidad(Especialidad.EntrenadorPersonal)
                .OrderBy(e => e.Usuario.Nombre).ToList();
            modelo.EntrenadorId ??= modelo.Entrenadores.FirstOrDefault()?.EntrenadorId;
            if (modelo.EntrenadorId is Guid id)
                modelo.Libres = citas.BuscarDisponibilidad(id, modelo.Dia).ToHashSet();
            modelo.MisCitas = citas.ListarProximasDeCliente(UsuarioActual());
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View("Cliente", modelo);
    }

    // El Id del entrenador es el mismo que el de su usuario (migración EntrenadorUsuario)
    private IActionResult DelEntrenador(EstadoCita? estado)
    {
        var modelo = new CitasEntrenadorViewModel { Estado = estado, Ahora = DateTime.Now };
        try
        {
            modelo.Citas = new CitaRepositorio(_db).ListarDeEntrenador(UsuarioActual(), estado);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View("Entrenador", modelo);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(Rol.Cliente))]
    public IActionResult Reservar(Guid entrenadorId, DateTime dia, DateTime? hora)
    {
        if (hora == null)
        {
            TempData["Error"] = "Elige un hueco libre.";
        }
        else
        {
            try
            {
                var cita = new CitaRepositorio(_db).Reservar(UsuarioActual(), entrenadorId, hora.Value);
                TempData["Ok"] = $"Cita reservada el {cita.FechaActividad.ToString("dddd d 'de' MMMM 'a las' HH:mm", Es)}.";
            }
            catch (ValidacionException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
            {
                TempData["Error"] = Errores.ErrorBaseDeDatos;
            }
        }
        return RedirectToAction(nameof(Index), new { entrenador = entrenadorId, dia = dia.ToString("yyyy-MM-dd") });
    }

    // Cada rol anula solo las suyas: el Id de quien anula sale de la cookie, nunca del formulario
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Anular(Guid citaId, EstadoCita? estado, Guid? entrenador, DateTime? dia)
    {
        try
        {
            var citas = new CitaRepositorio(_db);
            var cita = User.IsInRole(nameof(Rol.Entrenador))
                ? citas.AnularDeEntrenador(UsuarioActual(), citaId)
                : citas.AnularDeCliente(UsuarioActual(), citaId);
            TempData["Ok"] = $"Cita del {cita.FechaActividad.ToString("dddd d 'de' MMMM 'a las' HH:mm", Es)} anulada.";
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }
        return RedirectToAction(nameof(Index), new { estado, entrenador, dia = dia?.ToString("yyyy-MM-dd") });
    }

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
