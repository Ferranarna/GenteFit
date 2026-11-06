using GenteFit.Datos.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

public class CalendarController : Controller
{
    private readonly SesionRepositorio _sesion;

    public CalendarController(SesionRepositorio sesion)
    {
        _sesion = sesion;
    }

    public IActionResult Index(DateTime? dia)
    {
        DateTime diaElegido = dia ?? DateTime.Today;
        var sesiones = _sesion.Buscar(null, diaElegido);

        return View(sesiones);
    }
}