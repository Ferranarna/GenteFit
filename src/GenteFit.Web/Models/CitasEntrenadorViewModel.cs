using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

public class CitasEntrenadorViewModel
{
    public static readonly (EstadoCita? Valor, string Texto)[] Opciones =
    [
        (null, "Todas"),
        (EstadoCita.Programada, "Confirmadas"),
        (EstadoCita.Cancelada, "Canceladas")
    ];

    public EstadoCita? Estado { get; set; }
    public List<Cita> Citas { get; set; } = [];
    public DateTime Ahora { get; set; } = DateTime.Now;

    public bool SePuedeAnular(Cita cita) =>
       cita.Estado == EstadoCita.Programada && cita.FechaActividad > Ahora;
}