using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

// Con quién se reserva una cita: un cliente (si reserva el entrenador) o un entrenador personal (si reserva el cliente)
public record PersonaCita(Guid Id, string Nombre);

// Pantalla "Mis citas": la misma para el cliente y para el entrenador
public class MisCitasViewModel
{
    public static readonly (EstadoCita? Valor, string Texto)[] Opciones =
    [
        (null, "Todas"),
        (EstadoCita.Programada, "Confirmadas"),
        (EstadoCita.Cancelada, "Canceladas")
    ];

    public bool EsEntrenador { get; set; }
    public EstadoCita? Estado { get; set; }
    public List<Cita> Citas { get; set; } = [];
    public DateTime Ahora { get; set; } = DateTime.Now;

    // Tarjeta "Nueva cita"
    public bool PuedeReservar { get; set; }
    public bool Nueva { get; set; }
    public List<PersonaCita> Personas { get; set; } = [];
    public Guid? PersonaId { get; set; }
    public DateTime Dia { get; set; } = DateTime.Today;
    public HashSet<DateTime> Libres { get; set; } = [];

    // El otro en la cita: el cliente (si lo mira el entrenador) o el entrenador (si lo mira el cliente)
    public string EtiquetaOtro => EsEntrenador ? "Cliente" : "Entrenador";
    public Usuario Otro(Cita cita) => EsEntrenador ? cita.Usuario : cita.Entrenador.Usuario;

    // Las horas en que puede empezar una cita ese día: cada 30 minutos desde las 9:00
    public IEnumerable<DateTime> Huecos()
    {
        for (var hora = Dia.Date.AddHours(9); hora < Dia.Date.AddDays(1); hora = hora.AddMinutes(30))
            yield return hora;
    }

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