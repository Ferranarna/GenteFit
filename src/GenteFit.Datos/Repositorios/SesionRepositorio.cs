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
            .Include(s => s.Entrenador)
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha.Date == dia.Date);

        if (actividadId != null)
            consulta = consulta.Where(s => s.ActividadId == actividadId);

        return consulta.OrderBy(s => s.Fecha).ToList();
    }

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

    private void Validar(Sesion sesion)
    {
        var actividad = _db.Actividades.Find(sesion.ActividadId)
            ?? throw new ValidacionException("La actividad no existe.");
        var entrenador = _db.Entrenadores.Find(sesion.EntrenadorId)
            ?? throw new ValidacionException("El entrenador no existe.");
        if (_db.Salas.Find(sesion.SalaId) == null)
            throw new ValidacionException("La sala no existe.");

        if (!entrenador.Activo)
            throw new ValidacionException("El entrenador está dado de baja.");
        if (entrenador.Especialidad != actividad.Especialidad)
            throw new ValidacionException("La especialidad del entrenador no coincide con la de la actividad.");

        Validador.MayorQueCero(sesion.Duracion, "duración");
        var fin = sesion.Fecha.AddMinutes(sesion.Duracion);
        if (sesion.Fecha.Hour < HoraApertura || fin > sesion.Fecha.Date.AddDays(1))
            throw new ValidacionException("La sesión debe estar dentro del horario del centro (09:00 - 00:00).");
    }
}
