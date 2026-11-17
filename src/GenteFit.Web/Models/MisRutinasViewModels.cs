using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Models;

// Pantalla "Mis rutinas": las tarjetas de las rutinas del cliente y el editor de la que está abierta
public class MisRutinasViewModel
{
    public List<Rutina> Rutinas { get; set; } = [];

    // Rutina desplegada al cargar la página (tras guardar, añadir o quitar un ejercicio)
    public Guid? Abierta { get; set; }

    // Está desplegada la tarjeta "Nueva rutina" (por ejemplo, tras un error al crearla)
    public bool Nueva { get; set; }

    // Catálogo completo para el desplegable "Añadir ejercicio"
    public List<Ejercicio> Catalogo { get; set; } = [];
}

// Una fila de la tabla de series y repeticiones al pulsar "Guardar"
public class FilaSeries
{
    public Guid DetalleId { get; set; }
    public int Series { get; set; }
    public int Repeticiones { get; set; }
}

// Pantalla "Añadir a rutina" desde el catálogo de Ejercicios
public class AnadirARutinaViewModel
{
    public Ejercicio Ejercicio { get; set; } = null!;
    public List<Rutina> Rutinas { get; set; } = [];
}