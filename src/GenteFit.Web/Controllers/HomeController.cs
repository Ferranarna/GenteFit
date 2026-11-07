using System.Security.Claims;
using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Enums;
using GenteFit.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly GenteFitContext _db;

    public HomeController(GenteFitContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        var rol = User.FindFirstValue(ClaimTypes.Role)!;
        try
        {
            return View(rol, EstadisticasDe(rol));
        }
        catch (Exception)
        {
            ViewBag.Error = "No se pueden cargar las estadísticas. Comprueba la conexión con la base de datos.";
            return View(rol, new List<Estadistica>());
        }
    }

    // Estadísticas del resumen de cada rol (las que aún no existen, no salen)
    private List<Estadistica> EstadisticasDe(string rol)
    {
        var estadisticas = new List<Estadistica>();

        if (rol == nameof(Rol.Administrador))
        {
            var (activos, deBaja) = new UsuarioRepositorio(_db).ContarActivosYDeBaja();
            var (clientes, plantilla) = new UsuarioRepositorio(_db).ContarClientesYPlantilla();
            var (reservas, enEspera) = new ReservaRepositorio(_db).ContarDelMes(DateTime.Today);
            var (programadas, canceladas) = new SesionRepositorio(_db).ContarDelMes(DateTime.Today);

            estadisticas.Add(new Estadistica("usuarios", "Usuarios activos", activos.ToString(),
                "{0} usuarios de baja", "Ver usuarios", Resaltado: deBaja.ToString()));
            estadisticas.Add(new Estadistica("usuario", "Clientes activos", clientes.ToString(),
                "{0} trabajadores activos", "Ver clientes", Resaltado: plantilla.ToString()));
            estadisticas.Add(new Estadistica("marcador", "Reservas este mes", reservas.ToString(),
                "{0} en lista de espera", "Ver reservas", Resaltado: enEspera.ToString()));
            estadisticas.Add(new Estadistica("calendario", "Sesiones este mes", programadas.ToString(),
                "{0} canceladas", "Ver sesiones", Resaltado: canceladas.ToString()));
        }

        if (rol == nameof(Rol.Encargado))
        {
            var sesiones = new SesionRepositorio(_db);
            var (reservadas, plazas) = sesiones.OcupacionDeLaSemana(DateTime.Today);
            var porcentaje = plazas == 0 ? 0 : reservadas * 100 / plazas;
            var (sesionesSemana, salasEnUso) = sesiones.ContarDeLaSemana(DateTime.Today);
            var actividad = new ActividadRepositorio(_db).MasReservadaDelMes(DateTime.Today);
            var sala = new SalaRepositorio(_db).MasUsadaDelMes(DateTime.Today);

            estadisticas.Add(new Estadistica("informe", "Ocupación de sesiones", $"{porcentaje} %",
                "{0} plazas reservadas esta semana", "Ver sesiones",
                Porcentaje: porcentaje, Resaltado: reservadas.ToString()));
            estadisticas.Add(actividad is { } a
                ? new Estadistica("actividad", "Actividad más reservada", a.Nombre,
                    "{0} reservas este mes", "Ver actividades", Resaltado: a.Reservas.ToString())
                : new Estadistica("actividad", "Actividad más reservada", "—",
                    "Sin reservas este mes", "Ver actividades"));
            estadisticas.Add(new Estadistica("calendario", "Sesiones esta semana", sesionesSemana.ToString(),
                "{0} salas en uso", "Ver sesiones", Resaltado: salasEnUso.ToString()));
            estadisticas.Add(sala is { } s
                ? new Estadistica("puerta", "Sala más usada", s.Nombre,
                    "{0} sesiones este mes", "Ver salas", Resaltado: s.Sesiones.ToString())
                : new Estadistica("puerta", "Sala más usada", "—",
                    "Sin sesiones este mes", "Ver salas"));
        }

        if (rol == nameof(Rol.Recepcionista))
        {
            var (clientesActivos, clientesDeBaja) = new UsuarioRepositorio(_db).ContarClientesActivosYDeBaja();
            var sesiones = new SesionRepositorio(_db);
            var (sesionesHoy, proxima) = sesiones.ContarDeHoy(DateTime.Now);
            var (libres, quedan) = sesiones.PlazasLibresHoy(DateTime.Now);
            var (reservasHoy, enEspera) = new ReservaRepositorio(_db).ContarDeHoy(DateTime.Today);

            estadisticas.Add(new Estadistica("usuarios", "Clientes activos", clientesActivos.ToString(),
                "{0} clientes de baja", "Ver clientes", Resaltado: clientesDeBaja.ToString()));
            estadisticas.Add(proxima is { } p
                ? new Estadistica("calendario", "Sesiones de hoy", sesionesHoy.ToString(),
                    "Próxima a las {0}", "Ver sesiones", Resaltado: p.ToString("HH:mm"))
                : new Estadistica("calendario", "Sesiones de hoy", sesionesHoy.ToString(),
                    "No quedan más sesiones hoy", "Ver sesiones"));
            estadisticas.Add(new Estadistica("puerta", "Plazas libres hoy", libres.ToString(),
                "En {0} sesiones que quedan hoy", "Ver sesiones", Resaltado: quedan.ToString()));
            estadisticas.Add(new Estadistica("marcador", "Reservas de hoy", reservasHoy.ToString(),
                "{0} en lista de espera", "Ver sesiones", Resaltado: enEspera.ToString()));
        }

        return estadisticas;
    }
}