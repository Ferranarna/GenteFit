using System.ComponentModel.DataAnnotations;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

// Datos del formulario de crear / modificar actividad (el mismo para las dos cosas)
public class ActividadFormularioViewModel
{
    // Vacío al crear; con el id de la actividad al modificar
    public Guid? ActividadId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Elige la especialidad que debe tener el entrenador.")]
    public Especialidad? Especialidad { get; set; }

    [StringLength(255, ErrorMessage = "La descripción no puede tener más de 255 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Elige la intensidad.")]
    public Intensidad? Intensidad { get; set; }

    [Range(1, 16, ErrorMessage = "Las plazas deben estar entre 1 y 16.")]
    public int LimiteUsuarios { get; set; } = 16;

    public bool EsNueva => ActividadId == null;
}
