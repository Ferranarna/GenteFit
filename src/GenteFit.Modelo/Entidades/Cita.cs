using GenteFit.Modelo.Enums;

namespace GenteFit.Modelo.Entidades;

public class Cita
{
    public Guid CitaId { get; set; }
    public DateTime FechaActividad { get; set; }
    public int Duracion { get; set; } = 30; // minutos, siempre 30
    public EstadoCita Estado { get; set; } = EstadoCita.Programada;

    public Guid UsuarioId { get; set; }
    public Guid EntrenadorId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Entrenador Entrenador { get; set; } = null!;
}