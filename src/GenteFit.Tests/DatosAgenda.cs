using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests;

// Crea actividades, salas, entrenadores, sesiones y reservas directamente en la base de pruebas.
// No pasa por los repositorios para poder fijar fechas y estados a medida, incluso en el pasado.
public static class DatosAgenda
{
    public static Actividad CrearActividad(GenteFitContext db, string nombre = "Yoga", int plazas = 10)
    {
        var actividad = new Actividad
        {
            NombreActividad = nombre,
            Intensidad = Intensidad.Media,
            Especialidad = Especialidad.Yoga,
            LimiteUsuarios = plazas
        };
        db.Actividades.Add(actividad);
        db.SaveChanges();
        return actividad;
    }

    public static Sala CrearSala(GenteFitContext db, string nombre)
    {
        var sala = new Sala { NombreSala = nombre, Ubicacion = Ubicacion.Planta1 };
        db.Salas.Add(sala);
        db.SaveChanges();
        return sala;
    }

    // Un entrenador es un usuario con rol Entrenador más su ficha de entrenador
    public static Entrenador CrearEntrenador(GenteFitContext db, string prefijo)
    {
        var usuario = DatosPrueba.CrearUsuario(new UsuarioRepositorio(db),
            email: $"{prefijo}@prueba.com", dni: prefijo, rol: Rol.Entrenador);
        var entrenador = new Entrenador { EntrenadorId = usuario.UsuarioId, Especialidad = Especialidad.Yoga };
        db.Entrenadores.Add(entrenador);
        db.SaveChanges();
        return entrenador;
    }

    public static Sesion CrearSesion(GenteFitContext db, Actividad actividad, Sala sala, Entrenador entrenador,
        DateTime fecha, EstadoSesion estado = EstadoSesion.Programada)
    {
        var sesion = new Sesion
        {
            Fecha = fecha,
            Duracion = 60,
            Estado = estado,
            ActividadId = actividad.ActividadId,
            SalaId = sala.SalaId,
            EntrenadorId = entrenador.EntrenadorId
        };
        db.Sesiones.Add(sesion);
        db.SaveChanges();
        return sesion;
    }

    public static void CrearReservas(GenteFitContext db, Sesion sesion, IEnumerable<Usuario> clientes, EstadoReserva estado)
    {
        foreach (var cliente in clientes)
        {
            db.Reservas.Add(new Reserva
            {
                FechaReserva = sesion.Fecha.AddDays(-2),
                Estado = estado,
                UsuarioId = cliente.UsuarioId,
                SesionId = sesion.SesionId
            });
        }
        db.SaveChanges();
    }

    // Una reserva con su fecha de reserva exacta, para fijar el orden de la lista de espera
    public static Reserva CrearReserva(GenteFitContext db, Sesion sesion, Usuario cliente,
        EstadoReserva estado, DateTime fechaReserva)
    {
        var reserva = new Reserva
        {
            FechaReserva = fechaReserva,
            Estado = estado,
            UsuarioId = cliente.UsuarioId,
            SesionId = sesion.SesionId
        };
        db.Reservas.Add(reserva);
        db.SaveChanges();
        return reserva;
    }
}