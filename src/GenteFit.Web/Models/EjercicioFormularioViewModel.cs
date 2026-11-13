using System.ComponentModel.DataAnnotations;

namespace GenteFit.Web.Models;

// Datos del formulario de crear / modificar ejercicio (el mismo para las dos cosas)
public class EjercicioFormularioViewModel
{
    // Vacío al crear; con el id del ejercicio al modificar
    public Guid? EjercicioId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Elige el grupo muscular.")]
    public string? GrupoMuscular { get; set; }

    [StringLength(255, ErrorMessage = "La descripción no puede tener más de 255 caracteres.")]
    public string? Descripcion { get; set; }

    public bool EsNuevo => EjercicioId == null;
}
