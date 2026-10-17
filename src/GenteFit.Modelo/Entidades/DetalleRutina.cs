namespace GenteFit.Modelo.Entidades;

public class DetalleRutina
{
    public Guid DetalleRutinaId { get; set; }
    public int Orden { get; set; }
    public int Series { get; set; }
    public int Repeticiones { get; set; }

    public Guid RutinaId { get; set; }
    public Guid EjercicioId { get; set; }

    public Rutina Rutina { get; set; } = null!;
    public Ejercicio Ejercicio { get; set; } = null!;
}