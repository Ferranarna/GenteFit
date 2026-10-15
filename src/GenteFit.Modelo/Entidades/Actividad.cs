using GenteFit.Modelo.Enums;

namespace GenteFit.Modelo.Entidades;

public class Actividad
{
    public Guid ActividadId { get; set; }
    public string NombreActividad { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public Intensidad Intensidad { get; set; }
    public int LimiteUsuarios { get; set; } = 16;
    public Especialidad Especialidad { get; set; }
    public string? UrlImagen { get; set; }

    public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
}