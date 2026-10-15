using GenteFit.Modelo.Enums;

namespace GenteFit.Modelo.Entidades;

public class Reserva
{
    public Guid ReservaId { get; set; }
    public DateTime FechaReserva { get; set; }
    public EstadoReserva Estado { get; set; }

    public Guid UsuarioId { get; set; }
    public Guid SesionId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Sesion Sesion { get; set; } = null!;
}