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

    // Primero lo que queda por venir (o en curso), de la más próxima a la más lejana;
    // después las finalizadas, de la más reciente a la más antigua.
    public static List<Cita> Ordenar(IEnumerable<Cita> citas, DateTime ahora)
    {
        var lista = citas.ToList();
        var porVenir = lista
            .Where(c => c.FechaActividad.AddMinutes(c.Duracion) > ahora)
            .OrderBy(c => c.FechaActividad);
        var finalizadas = lista
            .Where(c => c.FechaActividad.AddMinutes(c.Duracion) <= ahora)
            .OrderByDescending(c => c.FechaActividad);
        return porVenir.Concat(finalizadas).ToList();
    }
}