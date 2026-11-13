using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Models;

public class ReservasDeSesionViewModel
{
    public Sesion Sesion { get; set; } = null!;
    public List<Reserva> Confirmadas { get; set; } = [];
    public List<Reserva> EnEspera { get; set; } = [];   // ya viene por orden de llegada

    // Para volver al mismo día y filtro de la pantalla Sesiones
    public DateTime Dia { get; set; }
    public Guid? ActividadId { get; set; }

    public int Plazas => Sesion.Actividad.LimiteUsuarios;
    public int Libres => Math.Max(0, Plazas - Confirmadas.Count);
}