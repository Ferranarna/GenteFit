using System.ComponentModel.DataAnnotations;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Models;

public class SesionFormularioViewModel
{
    public Guid? SesionId { get; set; }

    [Required(ErrorMessage = "Elige la actividad.")]
    public Guid? ActividadId { get; set; }

    [Required(ErrorMessage = "Elige la sala.")]
    public Guid? SalaId { get; set; }

    [Required(ErrorMessage = "Elige el entrenador.")]
    public Guid? EntrenadorId { get; set; }

    [Required(ErrorMessage = "Elige la fecha."), DataType(DataType.Date)]
    public DateTime? Fecha { get; set; }

    [Required(ErrorMessage = "Elige la hora de inicio.")]
    public TimeSpan? Hora { get; set; }

    [Range(15, 180, ErrorMessage = "La duración debe estar entre 15 y 180 minutos.")]
    public int Duracion { get; set; } = 60;

    public List<Actividad> Actividades { get; set; } = [];
    public List<Sala> Salas { get; set; } = [];
    public List<Entrenador> Entrenadores { get; set; } = [];

    public bool EsNueva => SesionId == null;
}