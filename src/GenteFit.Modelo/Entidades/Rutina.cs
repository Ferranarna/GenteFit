namespace GenteFit.Modelo.Entidades;

public class Rutina
{
    public Guid RutinaId { get; set; }
    public string NombreRutina { get; set; } = string.Empty;

    public Guid UsuarioId { get; set; }

    // Quién la creó: el entrenador que se la hizo al cliente; null si la hizo el propio cliente
    public Guid? EntrenadorId { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Entrenador? Entrenador { get; set; }
    public ICollection<DetalleRutina> Detalles { get; set; } = new List<DetalleRutina>();
}