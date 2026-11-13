using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Models;

// Datos que el controlador de Ejercicios le pasa a la vista
public class EjerciciosViewModel
{
    // Texto escrito en el buscador (solo lo usa el encargado)
    public string? Buscar { get; set; }

    // Grupo muscular elegido en el filtro (cliente y entrenador); null = todos
    public string? Grupo { get; set; }

    // El encargado ve la tabla con los botones de gestión; el resto, las tarjetas
    public bool PuedeEditar { get; set; }

    public List<Ejercicio> Ejercicios { get; set; } = [];

    // Grupos musculares que se ofrecen en el filtro y en el formulario
    public static readonly string[] Grupos =
        ["Brazos", "Core", "Espalda", "Glúteos", "Hombros", "Pecho", "Piernas"];
}
