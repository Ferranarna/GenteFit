using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

public class Entrenador
{
    public Guid EntrenadorId { get; set; } 
    public Especialidad Especialidad { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}