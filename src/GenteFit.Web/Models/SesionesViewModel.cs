using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Models;

public class SesionesViewModel
{
    public DateTime Dia { get; set; } = DateTime.Today;
    public Guid? ActividadId { get; set; }

    public List<Actividad> Actividades { get; set; } = [];
    public HashSet<int> DiasConSesiones { get; set; } = [];
    public List<SesionDelDia> Sesiones { get; set; } = [];

    // Solo el cliente ve los botones Reservar / Lista de espera
    public bool PuedeReservar { get; set; }

    public bool PuedeEditar { get; set; }

    public DateTime PrimeroDelMes => new(Dia.Year, Dia.Month, 1);

    public int HuecosAntesDelUno => ((int)PrimeroDelMes.DayOfWeek + 6) % 7;
}
