using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

public class MisReservasViewModel
{
    public static readonly (EstadoReserva? Valor, string Texto)[] Opciones =
   [
       (null, "Todas"),
        (EstadoReserva.Confirmada, "Confirmadas"),
        (EstadoReserva.ListaEspera, "En lista de espera"),
        (EstadoReserva.Cancelada, "Canceladas")
   ];

    public EstadoReserva? Estado { get; set; }
    public List<MiReserva> Reservas { get; set; } = [];

    public DateTime Ahora { get; set; } = DateTime.Now;

    public bool SePuedeAnular(MiReserva fila) =>
       fila.Reserva.Estado != EstadoReserva.Cancelada && fila.Reserva.Sesion.Fecha > Ahora;

    public static string TextoEstado(EstadoReserva estado) => estado switch
    {
        EstadoReserva.Confirmada => "Confirmada",
        EstadoReserva.ListaEspera => "Lista de espera",
        EstadoReserva.Cancelada => "Cancelada",
        _ => estado.ToString()
    };
}