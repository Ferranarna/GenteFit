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
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
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

        if (rol == nameof(Rol.Entrenador))
        {
            // El id del entrenador es el mismo que el de su usuario
            var entrenadorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var especialidad = new EntrenadorRepositorio(_db).ObtenerPorUsuario(entrenadorId)?.Especialidad;

            estadisticas.AddRange(especialidad == Especialidad.EntrenadorPersonal
                ? EstadisticasDeEntrenadorPersonal(entrenadorId)
                : EstadisticasDeEntrenadorDeClases(entrenadorId));
        }

        if (rol == nameof(Rol.Cliente))
        {
            var clienteId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            estadisticas.AddRange(EstadisticasDeCliente(clienteId));
        }

        return estadisticas;
    }
    // Entrenador de clases (yoga, spinning...): sus sesiones y las reservas de sus alumnos
    private List<Estadistica> EstadisticasDeEntrenadorDeClases(Guid entrenadorId)
    {
        var sesiones = new SesionRepositorio(_db);
        var (sesionesSemana, _) = sesiones.ContarDeLaSemana(DateTime.Today, entrenadorId);
        var proximaSesion = sesiones.ObtenerDeEntrenador(entrenadorId).FirstOrDefault(s => s.Fecha >= DateTime.Now);
        var (sesionesHoy, proximaHoy) = sesiones.ContarDeHoy(DateTime.Now, entrenadorId);
        var (reservadas, plazas) = sesiones.OcupacionDeLaSemana(DateTime.Today, entrenadorId);
        var porcentaje = plazas == 0 ? 0 : reservadas * 100 / plazas;
        var (alumnos, reservas) = new ReservaRepositorio(_db).ContarAlumnosDeLaSemana(entrenadorId, DateTime.Today);

        return new List<Estadistica>
        {
            proximaSesion != null
                ? new Estadistica("calendario", "Mis sesiones esta semana", sesionesSemana.ToString(),
                    "Próxima el {0}", "Ver sesiones", Resaltado: proximaSesion.Fecha.ToString("dd/MM HH:mm"))
                : new Estadistica("calendario", "Mis sesiones esta semana", sesionesSemana.ToString(),
                    "No tienes sesiones próximas", "Ver sesiones"),
            proximaHoy is { } hora
                ? new Estadistica("reloj", "Mis sesiones de hoy", sesionesHoy.ToString(),
                    "Próxima a las {0}", "Ver sesiones", Resaltado: hora.ToString("HH:mm"))
                : new Estadistica("reloj", "Mis sesiones de hoy", sesionesHoy.ToString(),
                    "No quedan más sesiones hoy", "Ver sesiones"),
            new Estadistica("informe", "Ocupación de mis sesiones", $"{porcentaje} %",
                "{0} plazas reservadas esta semana", "Ver sesiones",
                Porcentaje: porcentaje, Resaltado: reservadas.ToString()),
            new Estadistica("usuarios", "Alumnos esta semana", alumnos.ToString(),
                "{0} reservas en total", "Ver sesiones", Resaltado: reservas.ToString())
        };
    }

    // Entrenador personal: sus citas y sus clientes
    private List<Estadistica> EstadisticasDeEntrenadorPersonal(Guid entrenadorId)
    {
        var citas = new CitaRepositorio(_db);
        var pendientes = citas.ObtenerDeEntrenador(entrenadorId); // citas futuras, ordenadas por fecha
        var proxima = pendientes.FirstOrDefault();
        var clientes = citas.ObtenerClientesDeEntrenador(entrenadorId);
        var citasHoy = pendientes.Where(c => c.FechaActividad.Date == DateTime.Today).ToList();
        var enSieteDias = pendientes.Count(c => c.FechaActividad < DateTime.Now.AddDays(7));

        return new List<Estadistica>
        {
            citasHoy.Count > 0
                ? new Estadistica("reloj", "Citas que quedan hoy", citasHoy.Count.ToString(),
                    "Próxima a las {0}", "Ver citas", Resaltado: citasHoy[0].FechaActividad.ToString("HH:mm"))
                : new Estadistica("reloj", "Citas que quedan hoy", "0",
                    "No quedan más citas hoy", "Ver citas"),
            new Estadistica("calendario", "Citas próximos 7 días", enSieteDias.ToString(),
                "{0} citas pendientes en total", "Ver citas", Resaltado: pendientes.Count.ToString()),
            new Estadistica("usuarios", "Mis clientes", clientes.Count.ToString(),
                clientes.Count > 0 ? "Con citas programadas" : "Aún sin citas con clientes", "Ver clientes"),
            proxima != null
                ? new Estadistica("marcador", "Próxima cita", proxima.FechaActividad.ToString("dd/MM HH:mm"),
                    "Con {0}", "Ver citas", Resaltado: $"{proxima.Usuario.Nombre} {proxima.Usuario.Apellido1}")
                : new Estadistica("marcador", "Próxima cita", "—",
                    "Sin citas pendientes", "Ver citas")
        };
    }

    // Cliente: sus reservas, su próxima cita y sus rutinas
    private List<Estadistica> EstadisticasDeCliente(Guid clienteId)
    {
        var reservas = new ReservaRepositorio(_db).ObtenerDeCliente(clienteId); // no canceladas, por fecha
        var proximas = reservas
            .Where(r => r.Sesion.Estado == EstadoSesion.Programada && r.Sesion.Fecha >= DateTime.Now)
            .ToList();
        var proximaReserva = proximas.FirstOrDefault();
        var favorita = reservas
            .GroupBy(r => r.Sesion.Actividad.NombreActividad)
            .Select(g => new { Nombre = g.Key, Veces = g.Count() })
            .OrderByDescending(x => x.Veces).ThenBy(x => x.Nombre)
            .FirstOrDefault();

        var proximaCita = new CitaRepositorio(_db).ObtenerDeCliente(clienteId)
            .FirstOrDefault(c => c.FechaActividad >= DateTime.Now);

        var rutinas = new RutinaRepositorio(_db).ObtenerDeCliente(clienteId);
        var ejercicios = rutinas.Sum(r => r.Detalles.Count);

        return new List<Estadistica>
        {
            proximaReserva != null
                ? new Estadistica("marcador", "Mis próximas reservas", proximas.Count.ToString(),
                    "Próxima: {0}", "Ver reservas",
                    Resaltado: $"{proximaReserva.Sesion.Actividad.NombreActividad} · {proximaReserva.Sesion.Fecha:dd/MM HH:mm}")
                : new Estadistica("marcador", "Mis próximas reservas", "0",
                    "No tienes reservas próximas", "Ver sesiones"),
            proximaCita != null
                ? new Estadistica("reloj", "Mi próxima cita", proximaCita.FechaActividad.ToString("dd/MM HH:mm"),
                    "Con {0}", "Ver citas",
                    Resaltado: $"{proximaCita.Entrenador.Usuario.Nombre} {proximaCita.Entrenador.Usuario.Apellido1}")
                : new Estadistica("reloj", "Mi próxima cita", "—",
                    "No tienes citas pendientes", "Ver citas"),
            new Estadistica("lista", "Mis rutinas", rutinas.Count.ToString(),
                rutinas.Count > 0 ? "{0} ejercicios en total" : "Aún no has creado rutinas",
                "Ver rutinas", Resaltado: ejercicios.ToString()),
            favorita != null
                ? new Estadistica("actividad", "Mi actividad más reservada", favorita.Nombre,
                    "{0} reservas", "Ver actividades", Resaltado: favorita.Veces.ToString())
                : new Estadistica("actividad", "Mi actividad más reservada", "—",
                    "Aún sin reservas", "Ver actividades")
        };
    }
}