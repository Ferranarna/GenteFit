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

[Authorize(Roles = nameof(Rol.Cliente))]
public class MisReservasController : Controller
{
    private readonly GenteFitContext _db;

    public MisReservasController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(EstadoReserva? estado)
    {
        var modelo = new MisReservasViewModel { Estado = estado, Ahora = DateTime.Now };

        try
        {
            modelo.Reservas = new ReservaRepositorio(_db).ListarDeCliente(UsuarioActual(), estado);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Anular(Guid reservaId, EstadoReserva? estado)
    {
        try
        {
            var promovida = new ReservaRepositorio(_db).AnularDeCliente(UsuarioActual(), reservaId);
            TempData["Ok"] = promovida != null
                ? "Reserva anulada. Tu plaza ha pasado a la primera persona de la lista de espera."
                : "Reserva anulada.";
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }

        return RedirectToAction(nameof(Index), new { estado });
    }

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
