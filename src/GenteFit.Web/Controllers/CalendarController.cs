using GenteFit.Datos.Repositorios;
using GenteFit.Web.Models;
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

        var modelo = sesiones.Select(s => new SesionCalendarViewModel

        {
            SesionId = s.SesionId,
            Inicio = s.Fecha,
            DuracionMinutos = s.Duracion,
            Actividad = s.Actividad.NombreActividad,
            Intensidad = s.Actividad.Intensidad,
            Sala = s.Sala.NombreSala,
            Monitor = s.Entrenador.Usuario.Nombre + " " + s.Entrenador.Usuario.Apellido1,
            PlazasTotales = s.Actividad.LimiteUsuarios,
            PlazasLibres = _sesion.PlazasDisponibles(s.SesionId)
        }).ToList();

        return View(sesiones);
    }
}