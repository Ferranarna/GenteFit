using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Models;

public class MisCitasViewModel
{
    public List<Entrenador> Entrenadores { get; set; } = [];
    public Guid? EntrenadorId { get; set; }
    public DateTime Dia { get; set; } = DateTime.Today;

    public HashSet<DateTime> Libres { get; set; } = [];
    public List<Cita> MisCitas { get; set; } = [];

    public IEnumerable<DateTime> Huecos()
    {
        for (var hora = Dia.Date.AddHours(9); hora < Dia.Date.AddDays(1); hora = hora.AddMinutes(30))
            yield return hora;
    }
}