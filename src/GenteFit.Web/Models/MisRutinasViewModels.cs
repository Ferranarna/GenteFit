using System.ComponentModel.DataAnnotations;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Models;

// Pantalla "Mis rutinas": las tarjetas de las rutinas del cliente
public class MisRutinasViewModel
{
    public List<Rutina> Rutinas { get; set; } = [];
}

// Pantalla de detalle de una rutina: sus ejercicios y el catálogo para añadir más
public class RutinaDetalleViewModel
{
    public Rutina Rutina { get; set; } = null!;

    // Los ejercicios de la rutina en su orden
    public List<DetalleRutina> Detalles => Rutina.Detalles.OrderBy(d => d.Orden).ToList();

    // Catálogo completo para el desplegable "Añadir ejercicio"
    public List<Ejercicio> Catalogo { get; set; } = [];
}

// Una fila de la tabla de series y repeticiones al pulsar "Guardar cambios"
public class FilaSeries
{
    public Guid DetalleId { get; set; }
    public int Series { get; set; }
    public int Repeticiones { get; set; }
}

// Formulario "Nueva rutina"
public class NuevaRutinaViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
    public string? Nombre { get; set; }
}

// Pantalla "Añadir a rutina" desde el catálogo de Ejercicios
public class AnadirARutinaViewModel
{
    public Ejercicio Ejercicio { get; set; } = null!;
    public List<Rutina> Rutinas { get; set; } = [];
}
