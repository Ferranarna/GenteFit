using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos.Repositorios;

public class SesionRepositorio : Repositorio<Sesion>
{
    private const int HoraApertura = 9;

    public SesionRepositorio(GenteFitContext db) : base(db) { }

    public override Sesion Crear(Sesion sesion)
    {
        Validar(sesion);
        sesion.Estado = EstadoSesion.Programada;
        return base.Crear(sesion);
    }

    public override void Actualizar(Sesion sesion)
    {
        Validar(sesion);
        base.Actualizar(sesion);
    }

    public List<Sesion> Buscar(Guid? actividadId, DateTime dia)
    {
        var consulta = _db.Sesiones
            .Include(s => s.Actividad)
            .Include(s => s.Sala)
            .Include(s => s.Entrenador).ThenInclude(e => e.Usuario)
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha.Date == dia.Date);

        if (actividadId != null)
            consulta = consulta.Where(s => s.ActividadId == actividadId);

        return consulta.OrderBy(s => s.Fecha).ToList();
    }

    public List<Sesion> ObtenerDeEntrenador(Guid entrenadorId) =>
        _db.Sesiones
            .Include(s => s.Actividad)
            .Include(s => s.Sala)
            .Where(s => s.EntrenadorId == entrenadorId
                && s.Estado == EstadoSesion.Programada
                && s.Fecha >= DateTime.Today)
            .OrderBy(s => s.Fecha)
            .ToList();

    public int PlazasDisponibles(Guid sesionId)
    {
        var sesion = _db.Sesiones.Include(s => s.Actividad).First(s => s.SesionId == sesionId);
        int confirmadas = _db.Reservas.Count(r => r.SesionId == sesionId && r.Estado == EstadoReserva.Confirmada);
        return Math.Max(0, sesion.Actividad.LimiteUsuarios - confirmadas);
    }

    public List<Usuario> Cancelar(Guid sesionId)
    {
        var sesion = _db.Sesiones
            .Include(s => s.Reservas).ThenInclude(r => r.Usuario)
            .FirstOrDefault(s => s.SesionId == sesionId)
            ?? throw new ValidacionException("La sesión no existe.");

        sesion.Estado = EstadoSesion.Cancelada;

        var afectados = new List<Usuario>();
        foreach (var reserva in sesion.Reservas.Where(r => r.Estado != EstadoReserva.Cancelada))
        {
            reserva.Estado = EstadoReserva.Cancelada;
            afectados.Add(reserva.Usuario);
        }

        _db.SaveChanges();
        return afectados;
    }

    // Estadística: plazas reservadas y plazas totales de las sesiones programadas de la semana
    public (int Reservadas, int Plazas) OcupacionDeLaSemana(DateTime fecha)
    {
        // Semana de lunes a domingo
        var inicio = fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
        var fin = inicio.AddDays(7);

        int plazas = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin)
            .Sum(s => s.Actividad.LimiteUsuarios);
        int reservadas = _db.Reservas
            .Count(r => r.Estado == EstadoReserva.Confirmada
                && r.Sesion.Estado == EstadoSesion.Programada
                && r.Sesion.Fecha >= inicio && r.Sesion.Fecha < fin);

        return (reservadas, plazas);
    }

    // Estadística: sesiones programadas de la semana y cuántas salas distintas usan
    public (int Sesiones, int SalasEnUso) ContarDeLaSemana(DateTime fecha)
    {
        // Semana de lunes a domingo
        var inicio = fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
        var fin = inicio.AddDays(7);

        var sesiones = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin);

        return (sesiones.Count(), sesiones.Select(s => s.SalaId).Distinct().Count());
    }

    // Estadística: sesiones programadas de hoy y hora de la próxima (null si no queda ninguna)
    public (int Sesiones, DateTime? Proxima) ContarDeHoy(DateTime ahora)
    {
        var inicio = ahora.Date;
        var fin = inicio.AddDays(1);
        var deHoy = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin);

        return (deHoy.Count(), deHoy.Where(s => s.Fecha >= ahora).Min(s => (DateTime?)s.Fecha));
    }

    // Estadística: plazas libres y sesiones que quedan hoy (las que aún no han empezado)
    public (int Libres, int Sesiones) PlazasLibresHoy(DateTime ahora)
    {
        var fin = ahora.Date.AddDays(1);
        var quedan = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= ahora && s.Fecha < fin);

        int plazas = quedan.Sum(s => s.Actividad.LimiteUsuarios);
        int reservadas = _db.Reservas
            .Count(r => r.Estado == EstadoReserva.Confirmada
                && r.Sesion.Estado == EstadoSesion.Programada
                && r.Sesion.Fecha >= ahora && r.Sesion.Fecha < fin);

        return (Math.Max(0, plazas - reservadas), quedan.Count());
    }
    private void Validar(Sesion sesion)
    {
        var actividad = _db.Actividades.Find(sesion.ActividadId)
            ?? throw new ValidacionException("La actividad no existe.");
        var entrenador = _db.Entrenadores.Include(e => e.Usuario).FirstOrDefault(e => e.EntrenadorId == sesion.EntrenadorId)
            ?? throw new ValidacionException("El entrenador no existe.");
        if (_db.Salas.Find(sesion.SalaId) == null)
            throw new ValidacionException("La sala no existe.");

        if (!entrenador.Usuario.Activo)
            throw new ValidacionException("El entrenador está dado de baja.");
        if (entrenador.Especialidad != actividad.Especialidad)
            throw new ValidacionException("La especialidad del entrenador no coincide con la de la actividad.");

        Validador.MayorQueCero(sesion.Duracion, "duración");
        var fin = sesion.Fecha.AddMinutes(sesion.Duracion);
        if (sesion.Fecha.Hour < HoraApertura || fin > sesion.Fecha.Date.AddDays(1))
            throw new ValidacionException("La sesión debe estar dentro del horario del centro (09:00 - 00:00).");
    }

    // Estadística: sesiones del mes de la fecha indicada
    public (int Programadas, int Canceladas) ContarDelMes(DateTime fecha)
    {
        var inicio = new DateTime(fecha.Year, fecha.Month, 1);
        var fin = inicio.AddMonths(1);
        var delMes = _db.Sesiones.Where(s => s.Fecha >= inicio && s.Fecha < fin);

        return (delMes.Count(s => s.Estado == EstadoSesion.Programada),
                delMes.Count(s => s.Estado == EstadoSesion.Cancelada));
    }
}
