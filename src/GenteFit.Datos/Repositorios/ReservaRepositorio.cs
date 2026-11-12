using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos.Repositorios;

public class ReservaRepositorio : Repositorio<Reserva>
{
    public ReservaRepositorio(GenteFitContext db) : base(db) { }

    // Reserva una plaza. Si la sesión está llena, la reserva queda en LISTAESPERA.
    public Reserva Reservar(Guid usuarioId, Guid sesionId)
    {
        var sesion = _db.Sesiones.Include(s => s.Actividad).FirstOrDefault(s => s.SesionId == sesionId)
            ?? throw new ValidacionException("La sesión no existe.");
        if (sesion.Estado == EstadoSesion.Cancelada)
            throw new ValidacionException("La sesión está cancelada.");

        if (sesion.Fecha <= DateTime.Now)
            throw new ValidacionException("La sesión ya ha empezado.");

        var usuario = _db.Usuarios.Find(usuarioId)
            ?? throw new ValidacionException("El cliente no existe.");
        if (!usuario.Activo)
            throw new ValidacionException("El cliente está dado de baja.");

        int confirmadas = _db.Reservas.Count(r => r.SesionId == sesionId && r.Estado == EstadoReserva.Confirmada);
        var estado = confirmadas < sesion.Actividad.LimiteUsuarios ? EstadoReserva.Confirmada : EstadoReserva.ListaEspera;

        var reserva = _db.Reservas.FirstOrDefault(r => r.UsuarioId == usuarioId && r.SesionId == sesionId);
        if (reserva != null)
        {
            if (reserva.Estado != EstadoReserva.Cancelada)
                throw new ValidacionException("El cliente ya tiene reservada esta sesión.");

            reserva.Estado = estado;
            reserva.FechaReserva = DateTime.Now; // se coloca al final de la cola
        }
        else
        {
            reserva = new Reserva
            {
                UsuarioId = usuarioId,
                SesionId = sesionId,
                FechaReserva = DateTime.Now,
                Estado = estado
            };
            _db.Reservas.Add(reserva);
        }

        _db.SaveChanges();
        return reserva;
    }

    public Reserva? Anular(Guid reservaId)
    {
        var reserva = _db.Reservas.Find(reservaId)
            ?? throw new ValidacionException("La reserva no existe.");
        if (reserva.Estado == EstadoReserva.Cancelada)
            return null;

        bool teniaPlaza = reserva.Estado == EstadoReserva.Confirmada;
        reserva.Estado = EstadoReserva.Cancelada;

        Reserva? promovida = null;
        if (teniaPlaza)
        {
            promovida = _db.Reservas
                .Include(r => r.Usuario)
                .Where(r => r.SesionId == reserva.SesionId && r.Estado == EstadoReserva.ListaEspera)
                .OrderBy(r => r.FechaReserva)
                .FirstOrDefault();

            if (promovida != null)
                promovida.Estado = EstadoReserva.Confirmada;
        }

        _db.SaveChanges();
        return promovida;
    }

    // Comprueba que es suya, que no estaba ya cancelada y que la sesión no ha empezado;
    // después usa Anular, que promociona al primero de la lista de espera si se libera una plaza.
    public Reserva? AnularDeCliente(Guid usuarioId, Guid reservaId)
    {
        var reserva = _db.Reservas
            .Include(r => r.Sesion)
            .FirstOrDefault(r => r.ReservaId == reservaId);

        if (reserva == null || reserva.UsuarioId != usuarioId)
            throw new ValidacionException("La reserva no existe.");
        if (reserva.Estado == EstadoReserva.Cancelada)
            throw new ValidacionException("La reserva ya está cancelada.");
        if (reserva.Sesion.Fecha <= DateTime.Now)
            throw new ValidacionException("La sesión ya ha empezado.");

        return Anular(reservaId);
    }

    public int? ObtenerPosicion(Guid reservaId)
    {
        var reserva = _db.Reservas.Find(reservaId)
            ?? throw new ValidacionException("La reserva no existe.");
        if (reserva.Estado != EstadoReserva.ListaEspera)
            return null;

        return _db.Reservas.Count(r => r.SesionId == reserva.SesionId
            && r.Estado == EstadoReserva.ListaEspera
            && r.FechaReserva < reserva.FechaReserva) + 1;
    }

    public List<Reserva> ListarConfirmadosDeEntrenador(Guid entrenadorId, Guid sesionId)
    {
        var sesion = _db.Sesiones.Find(sesionId)
            ?? throw new ValidacionException("La sesión no existe.");
        if (sesion.EntrenadorId != entrenadorId)
            throw new ValidacionException("La sesión no es de este entrenador.");

        return ListarConfirmados(sesionId);
    }

    public List<Reserva> ObtenerDeCliente(Guid usuarioId) =>
        _db.Reservas
            .Include(r => r.Sesion).ThenInclude(s => s.Actividad)
            .Where(r => r.UsuarioId == usuarioId && r.Estado != EstadoReserva.Cancelada)
            .OrderBy(r => r.Sesion.Fecha)
            .ToList();

    public List<MiReserva> ListarDeCliente(Guid usuarioId, EstadoReserva? estado = null)
    {
        var reservas = _db.Reservas
            .Include(r => r.Sesion).ThenInclude(s => s.Actividad)
            .Include(r => r.Sesion).ThenInclude(s => s.Sala)
            .Where(r => r.UsuarioId == usuarioId && (estado == null || r.Estado == estado))
            .OrderBy(r => r.Sesion.Fecha)
            .ToList();

        var sesionesEnEspera = reservas
            .Where(r => r.Estado == EstadoReserva.ListaEspera)
            .Select(r => r.SesionId)
            .ToList();
        var colas = _db.Reservas
            .Where(r => sesionesEnEspera.Contains(r.SesionId) && r.Estado == EstadoReserva.ListaEspera)
            .Select(r => new { r.SesionId, r.FechaReserva })
            .ToList();

        return reservas.Select(r =>
        {
            int? posicion = r.Estado == EstadoReserva.ListaEspera
                ? colas.Count(c => c.SesionId == r.SesionId && c.FechaReserva < r.FechaReserva) + 1
                : null;
            return new MiReserva(r, posicion);
        }).ToList();
    }

    public List<Reserva> ListarConfirmados(Guid sesionId) =>
        _db.Reservas
            .Include(r => r.Usuario)
            .Where(r => r.SesionId == sesionId && r.Estado == EstadoReserva.Confirmada)
            .OrderBy(r => r.FechaReserva)
            .ToList();

    public List<Reserva> ListarListaEspera(Guid sesionId) =>
        _db.Reservas
            .Include(r => r.Usuario)
            .Where(r => r.SesionId == sesionId && r.Estado == EstadoReserva.ListaEspera)
            .OrderBy(r => r.FechaReserva)
            .ToList();

    // Estadística: reservas de las sesiones del mes de la fecha indicada
    public (int Confirmadas, int EnEspera) ContarDelMes(DateTime fecha)
    {
        var inicio = new DateTime(fecha.Year, fecha.Month, 1);
        var fin = inicio.AddMonths(1);
        var delMes = _db.Reservas.Where(r => r.Sesion.Fecha >= inicio && r.Sesion.Fecha < fin);

        return (delMes.Count(r => r.Estado == EstadoReserva.Confirmada),
                delMes.Count(r => r.Estado == EstadoReserva.ListaEspera));
    }

    // Estadística: reservas de las sesiones de hoy
    public (int Confirmadas, int EnEspera) ContarDeHoy(DateTime fecha)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);
        var deHoy = _db.Reservas.Where(r => r.Sesion.Fecha >= inicio && r.Sesion.Fecha < fin);

        return (deHoy.Count(r => r.Estado == EstadoReserva.Confirmada),
                deHoy.Count(r => r.Estado == EstadoReserva.ListaEspera));
    }

    // Estadística: alumnos distintos y reservas confirmadas en las sesiones de un entrenador esta semana
    public (int Alumnos, int Reservas) ContarAlumnosDeLaSemana(Guid entrenadorId, DateTime fecha)
    {
        // Semana de lunes a domingo
        var inicio = fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
        var fin = inicio.AddDays(7);

        var confirmadas = _db.Reservas
            .Where(r => r.Estado == EstadoReserva.Confirmada
                && r.Sesion.Estado == EstadoSesion.Programada
                && r.Sesion.EntrenadorId == entrenadorId
                && r.Sesion.Fecha >= inicio && r.Sesion.Fecha < fin);

        return (confirmadas.Select(r => r.UsuarioId).Distinct().Count(), confirmadas.Count());
    }
}