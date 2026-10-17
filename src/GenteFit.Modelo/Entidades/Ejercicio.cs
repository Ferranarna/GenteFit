namespace GenteFit.Modelo.Entidades;

public class Ejercicio
{
    public Guid EjercicioId { get; set; }
    public string NombreEjercicio { get; set; } = string.Empty;
    public string? GrupoMuscular { get; set; }
    public string? Descripcion { get; set; }
    public string? UrlImagen { get; set; }
    
    public ICollection<DetalleRutina> Detalles { get; set; } = new List<DetalleRutina>();
}