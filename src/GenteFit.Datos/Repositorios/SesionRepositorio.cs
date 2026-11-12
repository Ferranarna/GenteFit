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

    public Sesion Programar(Sesion sesion, DateTime ahora)
    {
        if (sesion.Fecha <= ahora)
            throw new ValidacionException("No se puede programar una sesión en el pasado.");
        return Crear(sesion);
    }

    public void Modificar(Sesion cambios, DateTime ahora)
    {
        var sesion = _db.Sesiones.Find(cambios.SesionId)
            ?? throw new ValidacionException("La sesión no existe.");

        if (sesion.Estado == EstadoSesion.Cancelada)
            throw new ValidacionException("No se puede modificar una sesión cancelada.");
        if (sesion.Fecha <= ahora)
            throw new ValidacionException("No se puede modificar una sesión que ya ha empezado.");
        if (cambios.Fecha <= ahora)
            throw new ValidacionException("La nueva fecha y hora ya han pasado.");

        sesion.ActividadId = cambios.ActividadId;
        sesion.SalaId = cambios.SalaId;
        sesion.EntrenadorId = cambios.EntrenadorId;
        sesion.Fecha = cambios.Fecha;
        sesion.Duracion = cambios.Duracion;
        Actualizar(sesion);
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

    public Sesion? ObtenerConDetalle(Guid sesionId) =>
       _db.Sesiones
           .Include(s => s.Actividad)
           .Include(s => s.Sala)
           .Include(s => s.Entrenador).ThenInclude(e => e.Usuario)
           .FirstOrDefault(s => s.SesionId == sesionId);

    public List<SesionDelDia> ListarDelDia(DateTime dia, Guid? actividadId = null, Guid? usuarioId = null)
    {
        var inicio = dia.Date;
        var fin = inicio.AddDays(1);

        var sesiones = _db.Sesiones
            .Include(s => s.Actividad)
            .Include(s => s.Sala)
            .Include(s => s.Entrenador).ThenInclude(e => e.Usuario)
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin
                && (actividadId == null || s.ActividadId == actividadId))
            .OrderBy(s => s.Fecha)
            .ToList();

        var ids = sesiones.Select(s => s.SesionId).ToList();
        var reservas = _db.Reservas
            .Where(r => ids.Contains(r.SesionId) && r.Estado != EstadoReserva.Cancelada)
            .Select(r => new { r.SesionId, r.UsuarioId, r.Estado, r.FechaReserva })
            .ToList();

        return sesiones.Select(s =>
        {
            var deEsta = reservas.Where(r => r.SesionId == s.SesionId).ToList();
            var espera = deEsta.Where(r => r.Estado == EstadoReserva.ListaEspera)
                               .OrderBy(r => r.FechaReserva).ToList();
            var mia = usuarioId == null ? null : deEsta.FirstOrDefault(r => r.UsuarioId == usuarioId);
            int? posicion = mia?.Estado == EstadoReserva.ListaEspera ? espera.IndexOf(mia) + 1 : null;

            return new SesionDelDia(s,
                deEsta.Count(r => r.Estado == EstadoReserva.Confirmada),
                espera.Count,
                mia?.Estado,
                posicion);
        }).ToList();
    }

    public HashSet<int> DiasConSesiones(int anio, int mes, Guid? actividadId = null)
    {
        var inicio = new DateTime(anio, mes, 1);
        var fin = inicio.AddMonths(1);

        return _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin
                && (actividadId == null || s.ActividadId == actividadId))
            .Select(s => s.Fecha)
            .AsEnumerable()
            .Select(f => f.Day)
            .ToHashSet();
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

    public List<Usuario> Cancelar(Guid sesionId, DateTime ahora)
    {
        var sesion = _db.Sesiones
            .Include(s => s.Reservas).ThenInclude(r => r.Usuario)
            .FirstOrDefault(s => s.SesionId == sesionId)
            ?? throw new ValidacionException("La sesión no existe.");

        if (sesion.Estado == EstadoSesion.Cancelada)
            throw new ValidacionException("Esta sesión ya estaba cancelada.");

        if (sesion.Fecha <= ahora)
            throw new ValidacionException("No se puede cancelar una sesión que ya ha empezado.");

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
    // (si se indica un entrenador, solo las suyas)
    public (int Reservadas, int Plazas) OcupacionDeLaSemana(DateTime fecha, Guid? entrenadorId = null)
    {
        // Semana de lunes a domingo
        var inicio = fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
        var fin = inicio.AddDays(7);

        int plazas = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin
                && (entrenadorId == null || s.EntrenadorId == entrenadorId))
            .Sum(s => s.Actividad.LimiteUsuarios);
        int reservadas = _db.Reservas
            .Count(r => r.Estado == EstadoReserva.Confirmada
                && r.Sesion.Estado == EstadoSesion.Programada
                && r.Sesion.Fecha >= inicio && r.Sesion.Fecha < fin
                && (entrenadorId == null || r.Sesion.EntrenadorId == entrenadorId));

        return (reservadas, plazas);
    }

    // Estadística: sesiones programadas de la semana y cuántas salas distintas usan
    // (si se indica un entrenador, solo las suyas)
    public (int Sesiones, int SalasEnUso) ContarDeLaSemana(DateTime fecha, Guid? entrenadorId = null)
    {
        // Semana de lunes a domingo
        var inicio = fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
        var fin = inicio.AddDays(7);

        var sesiones = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin
                && (entrenadorId == null || s.EntrenadorId == entrenadorId));

        return (sesiones.Count(), sesiones.Select(s => s.SalaId).Distinct().Count());
    }

    // Estadística: sesiones programadas de hoy y hora de la próxima (null si no queda ninguna)
    // (si se indica un entrenador, solo las suyas)
    public (int Sesiones, DateTime? Proxima) ContarDeHoy(DateTime ahora, Guid? entrenadorId = null)
    {
        var inicio = ahora.Date;
        var fin = inicio.AddDays(1);
        var deHoy = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin
                && (entrenadorId == null || s.EntrenadorId == entrenadorId));

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
        var inicioDia = sesion.Fecha.Date;
        var finDia = inicioDia.AddDays(1);
        var choques = _db.Sesiones
            .Where(s => s.SesionId != sesion.SesionId
                && s.Estado == EstadoSesion.Programada
                && s.Fecha >= inicioDia && s.Fecha < finDia
                && (s.SalaId == sesion.SalaId || s.EntrenadorId == sesion.EntrenadorId))
            .AsEnumerable()
            .Where(s => s.Fecha < fin && sesion.Fecha < s.Fecha.AddMinutes(s.Duracion))
            .ToList();

        if (choques.Any(s => s.SalaId == sesion.SalaId))
            throw new ValidacionException("La sala ya está ocupada a esa hora.");
        if (choques.Any(s => s.EntrenadorId == sesion.EntrenadorId))
            throw new ValidacionException("El entrenador ya tiene otra sesión a esa hora.");
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
