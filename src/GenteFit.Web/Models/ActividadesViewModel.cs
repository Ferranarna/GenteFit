using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

// Datos que el controlador de Actividades le pasa a la vista
public class ActividadesViewModel
{
    // Texto escrito en el buscador (solo lo usa el encargado)
    public string? Buscar { get; set; }

    public string Orden { get; set; } = "nombre";

    // El encargado ve la tabla con los botones de gestión; el resto, las tarjetas
    public bool PuedeEditar { get; set; }

    public List<Actividad> Actividades { get; set; } = [];

    public static readonly (string Valor, string Texto)[] Ordenes =
    [
        ("nombre", "Nombre"),
        ("intensidad", "Intensidad")
    ];

    public static string TextoEspecialidad(Especialidad especialidad) => especialidad switch
    {
        Especialidad.Bodypump => "Body Pump",
        Especialidad.BodyCombat => "Body Combat",
        Especialidad.EntrenadorPersonal => "Entrenador personal",
        _ => especialidad.ToString()
    };

    public static string ClaseIntensidad(Intensidad intensidad) => intensidad switch
    {
        Intensidad.Alta => "gf-chip-baja",
        Intensidad.Media => "gf-chip-espera",
        _ => "gf-chip-activo"
    };
}