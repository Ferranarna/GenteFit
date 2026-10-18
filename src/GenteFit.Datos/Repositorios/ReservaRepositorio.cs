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

    public List<Reserva> ObtenerDeCliente(Guid usuarioId) =>
        _db.Reservas
            .Include(r => r.Sesion).ThenInclude(s => s.Actividad)
            .Where(r => r.UsuarioId == usuarioId && r.Estado != EstadoReserva.Cancelada)
            .OrderBy(r => r.Sesion.Fecha)
            .ToList();

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
}