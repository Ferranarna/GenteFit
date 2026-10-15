namespace GenteFit.Modelo.Entidades;

public class Rutina
{
    public Guid RutinaId { get; set; }
    public string NombreRutina { get; set; } = string.Empty;

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public ICollection<DetalleRutina> Detalles { get; set; } = new List<DetalleRutina>();
}