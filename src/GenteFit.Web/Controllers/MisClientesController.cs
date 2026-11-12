using System.Security.Claims;
using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Enums;
using GenteFit.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

// Pantalla Mis clientes: los clientes que tienen citas con el entrenador y la próxima de cada uno
[Authorize(Roles = nameof(Rol.Entrenador))]
public class MisClientesController : Controller
{
    private readonly GenteFitContext _db;

    public MisClientesController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        List<ClienteDeEntrenador> clientes = [];
        try
        {
            clientes = new CitaRepositorio(_db).ListarClientesDeEntrenador(UsuarioActual());
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View(clientes);
    }

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}