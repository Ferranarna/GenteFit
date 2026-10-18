using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos.Repositorios;

// Citas con entrenador personal: siempre 30 minutos, sin lista de espera
public class CitaRepositorio : Repositorio<Cita>
{
    private const int DuracionCita = 30;

    public CitaRepositorio(GenteFitContext db) : base(db) { }

    public Cita Reservar(Guid usuarioId, Guid entrenadorId, DateTime fecha)
    {
        var entrenador = _db.Entrenadores.Find(entrenadorId)
            ?? throw new ValidacionException("El entrenador no existe.");
        if (!entrenador.Activo || entrenador.Especialidad != Especialidad.EntrenadorPersonal)
            throw new ValidacionException("Las citas solo se pueden pedir con un entrenador personal activo.");

        if (!EstaLibre(entrenadorId, fecha))
            throw new ValidacionException("El entrenador ya tiene una cita a esa hora.");

        var cita = new Cita
        {
            UsuarioId = usuarioId,
            EntrenadorId = entrenadorId,
            FechaActividad = fecha,
            Duracion = DuracionCita,
            Estado = EstadoCita.Programada
        };
        return base.Crear(cita);
    }

    public void Anular(Guid citaId)
    {
        var cita = ObtenerPorId(citaId) ?? throw new ValidacionException("La cita no existe.");
        cita.Estado = EstadoCita.Cancelada;
        _db.SaveChanges();
    }

    public bool EstaLibre(Guid entrenadorId, DateTime fecha)
    {
        var desde = fecha.AddMinutes(-DuracionCita);
        var hasta = fecha.AddMinutes(DuracionCita);
        return !_db.Citas.Any(c =>
            c.EntrenadorId == entrenadorId &&
            c.Estado == EstadoCita.Programada &&
            c.FechaActividad > desde && c.FechaActividad < hasta);
    }

    public List<DateTime> BuscarDisponibilidad(Guid entrenadorId, DateTime dia)
    {
        var ocupadas = _db.Citas
            .Where(c => c.EntrenadorId == entrenadorId && c.Estado == EstadoCita.Programada && c.FechaActividad.Date == dia.Date)
            .Select(c => c.FechaActividad)
            .ToList();

        var libres = new List<DateTime>();
        for (var hora = dia.Date.AddHours(9); hora < dia.Date.AddDays(1); hora = hora.AddMinutes(DuracionCita))
        {
            if (!ocupadas.Any(o => Math.Abs((o - hora).TotalMinutes) < DuracionCita))
                libres.Add(hora);
        }
        return libres;
    }

    public List<Cita> ObtenerDeCliente(Guid usuarioId) =>
        _db.Citas
            .Include(c => c.Entrenador)
            .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoCita.Programada)
            .OrderBy(c => c.FechaActividad)
            .ToList();
}