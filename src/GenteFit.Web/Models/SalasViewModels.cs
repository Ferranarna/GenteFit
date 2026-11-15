using System.ComponentModel.DataAnnotations;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

// Pantalla Salas (solo encargado): tabla con buscador
public class SalasViewModel
{
    public string? Buscar { get; set; }
    public List<Sala> Salas { get; set; } = [];

    // Texto que ve el usuario para cada ubicación (el enum no lleva espacios)
    public static string TextoUbicacion(Ubicacion ubicacion) => ubicacion switch
    {
        Ubicacion.Planta1 => "Planta 1",
        Ubicacion.Planta2 => "Planta 2",
        _ => ubicacion.ToString()
    };
}

// Formulario de crear / modificar sala (el mismo para las dos cosas)
public class SalaFormularioViewModel
{
    public Guid? SalaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Elige la planta.")]
    public Ubicacion? Ubicacion { get; set; }

    public bool EsNueva => SalaId == null;
}

// Pantalla del informe de uso de salas
public class InformeSalasViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }

    // null si no se ha podido calcular (fechas mal o BD caída)
    public InformeSalas? Informe { get; set; }
}
