using GenteFit.Modelo.Enums;

namespace GenteFit.Modelo.Entidades;

public class Entrenador
{
    public Guid EntrenadorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido1 { get; set; } = string.Empty;
    public string? Apellido2 { get; set; }
    public bool Activo { get; set; } = true;
    public int Telefono { get; set; }
    public Especialidad Especialidad { get; set; }

    public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}