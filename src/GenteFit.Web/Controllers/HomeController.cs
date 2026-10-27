using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

public class HomeController : Controller
{
    private readonly GenteFitContext _db;

    public HomeController(GenteFitContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        try
        {
            ViewBag.Actividades = new ActividadRepositorio(_db).ObtenerTodos();
        }
        catch (Exception)
        {
            ViewBag.Error = "No se puede conectar con la base de datos. Comprueba que SQL Server está en marcha y que la conexión es correcta.";
        }
        return View();
    }
}