using GenteFit.Modelo.Enums;

namespace GenteFit.Modelo.Entidades;

public class Sesion
{
    public Guid SesionId { get; set; }
    public DateTime Fecha { get; set; }
    public int Duracion { get; set; } // minutos
    public EstadoSesion Estado { get; set; } = EstadoSesion.Programada;

    public Guid ActividadId { get; set; }
    public Guid SalaId { get; set; }
    public Guid EntrenadorId { get; set; }

    public Actividad Actividad { get; set; } = null!;
    public Sala Sala { get; set; } = null!;
    public Entrenador Entrenador { get; set; } = null!;
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}