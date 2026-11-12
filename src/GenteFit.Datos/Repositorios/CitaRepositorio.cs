using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos.Repositorios;

public class CitaRepositorio : Repositorio<Cita>
{
    private const int DuracionCita = 30;

    public CitaRepositorio(GenteFitContext db) : base(db) { }

    public Cita Reservar(Guid usuarioId, Guid entrenadorId, DateTime fecha)
    {
        var entrenador = _db.Entrenadores.Include(e => e.Usuario).FirstOrDefault(e => e.EntrenadorId == entrenadorId)
            ?? throw new ValidacionException("El entrenador no existe.");
        if (!entrenador.Usuario.Activo || entrenador.Especialidad != Especialidad.EntrenadorPersonal)
            throw new ValidacionException("Las citas solo se pueden pedir con un entrenador personal activo.");
        if (fecha <= DateTime.Now)
            throw new ValidacionException("Esa hora ya ha pasado.");

        var cliente = _db.Usuarios.Find(usuarioId)
            ?? throw new ValidacionException("El cliente no existe.");
        if (!cliente.Activo)
            throw new ValidacionException("El cliente está dado de baja.");

        if (_db.Citas.Any(c => c.UsuarioId == usuarioId
                && c.Estado == EstadoCita.Programada
                && c.FechaActividad > fecha.AddMinutes(-DuracionCita)
                && c.FechaActividad < fecha.AddMinutes(DuracionCita)))
            throw new ValidacionException("El cliente ya tiene una cita a esa hora.");

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

    public Cita AnularDeCliente(Guid usuarioId, Guid citaId) =>
        AnularSiSePuede(citaId, c => c.UsuarioId == usuarioId);

    public Cita AnularDeEntrenador(Guid entrenadorId, Guid citaId) =>
        AnularSiSePuede(citaId, c => c.EntrenadorId == entrenadorId);

    private Cita AnularSiSePuede(Guid citaId, Func<Cita, bool> esSuya)
    {
        var cita = ObtenerPorId(citaId);
        if (cita == null || !esSuya(cita))
            throw new ValidacionException("La cita no existe.");
        if (cita.Estado == EstadoCita.Cancelada)
            throw new ValidacionException("La cita ya está cancelada.");
        if (cita.FechaActividad <= DateTime.Now)
            throw new ValidacionException("La cita ya ha empezado.");

        Anular(citaId);
        return cita;
    }
    public List<Cita> ListarProximasDeCliente(Guid usuarioId)
    {
        var ahora = DateTime.Now;
        return _db.Citas
            .Include(c => c.Entrenador).ThenInclude(e => e.Usuario)
            .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoCita.Programada && c.FechaActividad > ahora)
            .OrderBy(c => c.FechaActividad)
            .ToList();
    }

    public List<Cita> ListarDeEntrenador(Guid entrenadorId, EstadoCita? estado = null) =>
        _db.Citas
            .Include(c => c.Usuario)
            .Where(c => c.EntrenadorId == entrenadorId && (estado == null || c.Estado == estado))
            .OrderBy(c => c.FechaActividad)
            .ToList();

    public List<ClienteDeEntrenador> ListarClientesDeEntrenador(Guid entrenadorId)
    {
        var ahora = DateTime.Now;
        var citas = _db.Citas
            .Include(c => c.Usuario)
            .Where(c => c.EntrenadorId == entrenadorId && c.Estado == EstadoCita.Programada)
            .ToList();

        return citas
            .GroupBy(c => c.UsuarioId)
            .Select(g => new ClienteDeEntrenador(
                g.First().Usuario,
                g.Where(c => c.FechaActividad > ahora).Min(c => (DateTime?)c.FechaActividad)))
            .OrderBy(x => x.Cliente.Nombre).ThenBy(x => x.Cliente.Apellido1)
            .ToList();
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
            if (hora > DateTime.Now && !ocupadas.Any(o => Math.Abs((o - hora).TotalMinutes) < DuracionCita))
                libres.Add(hora);
        }
        return libres;
    }

    public List<Cita> ObtenerDeCliente(Guid usuarioId) =>
        _db.Citas
            .Include(c => c.Entrenador).ThenInclude(e => e.Usuario)
            .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoCita.Programada)
            .OrderBy(c => c.FechaActividad)
            .ToList();

    public List<Cita> ObtenerDeEntrenador(Guid entrenadorId) =>
        _db.Citas
            .Include(c => c.Usuario)
            .Where(c => c.EntrenadorId == entrenadorId
                && c.Estado == EstadoCita.Programada
                && c.FechaActividad >= DateTime.Now)
            .OrderBy(c => c.FechaActividad)
            .ToList();

    public List<Usuario> ObtenerClientesDeEntrenador(Guid entrenadorId) =>
        _db.Citas
            .Where(c => c.EntrenadorId == entrenadorId && c.Estado == EstadoCita.Programada)
            .Select(c => c.Usuario)
            .Distinct()
            .OrderBy(u => u.Nombre)
            .ToList();
}