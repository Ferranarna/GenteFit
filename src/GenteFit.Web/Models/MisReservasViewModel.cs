using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

public class MisReservasViewModel
{
    public enum Filtro { Todas, Confirmadas, EnEspera, Finalizadas, Canceladas }

    public static readonly (Filtro Valor, string Texto)[] Opciones =
   [
       (Filtro.Todas, "Todas"),
        (Filtro.Confirmadas, "Confirmadas"),
        (Filtro.EnEspera, "En lista de espera"),
        (Filtro.Finalizadas, "Finalizadas"),
        (Filtro.Canceladas, "Canceladas")
   ];

    public Filtro Estado { get; set; }
    public List<MiReserva> Reservas { get; set; } = [];

    public DateTime Ahora { get; set; } = DateTime.Now;

    // La sesión ya ha terminado
    private static bool Terminada(MiReserva fila, DateTime ahora) =>
        fila.Reserva.Sesion.Fecha.AddMinutes(fila.Reserva.Sesion.Duracion) <= ahora;

    public bool HaFinalizado(MiReserva fila) => Terminada(fila, Ahora);

    public bool SePuedeAnular(MiReserva fila) =>
       fila.Reserva.Estado != EstadoReserva.Cancelada && fila.Reserva.Sesion.Fecha > Ahora;

    // Las reservas que cumplen el filtro elegido
    public static List<MiReserva> Filtrar(IEnumerable<MiReserva> reservas, Filtro filtro, DateTime ahora) =>
        reservas.Where(f =>
        {
            var cancelada = f.Reserva.Estado == EstadoReserva.Cancelada;
            var finalizada = !cancelada && Terminada(f, ahora);
            return filtro switch
            {
                Filtro.Confirmadas => f.Reserva.Estado == EstadoReserva.Confirmada && !finalizada,
                Filtro.EnEspera => f.Reserva.Estado == EstadoReserva.ListaEspera && !finalizada,
                Filtro.Finalizadas => finalizada,
                Filtro.Canceladas => cancelada,
                _ => true
            };
        }).ToList();

    // Primero lo que queda por venir (o en curso), de la más próxima a la más lejana;
    // después las finalizadas, de la más reciente a la más antigua.
    public static List<MiReserva> Ordenar(IEnumerable<MiReserva> reservas, DateTime ahora)
    {
        var lista = reservas.ToList();
        var porVenir = lista
            .Where(f => !Terminada(f, ahora))
            .OrderBy(f => f.Reserva.Sesion.Fecha);
        var finalizadas = lista
            .Where(f => Terminada(f, ahora))
            .OrderByDescending(f => f.Reserva.Sesion.Fecha);
        return porVenir.Concat(finalizadas).ToList();
    }

    public static string TextoEstado(EstadoReserva estado) => estado switch
    {
        EstadoReserva.Confirmada => "Confirmada",
        EstadoReserva.ListaEspera => "Lista de espera",
        EstadoReserva.Cancelada => "Cancelada",
        _ => estado.ToString()
    };

    // Lo que dice el estado de una reserva: depende también de si su sesión ya ha pasado
    public string Texto(MiReserva fila)
    {
        var estado = fila.Reserva.Estado;
        if (estado == EstadoReserva.Cancelada) return "Cancelada";
        if (HaFinalizado(fila)) return "Finalizada";
        if (fila.Reserva.Sesion.Fecha <= Ahora) return "En curso";
        return fila.Posicion is int p ? $"{TextoEstado(estado)} · posición {p}" : TextoEstado(estado);
    }

    public string ClaseChip(MiReserva fila)
    {
        var estado = fila.Reserva.Estado;
        if (estado == EstadoReserva.Cancelada) return "gf-chip-baja";
        if (fila.Reserva.Sesion.Fecha <= Ahora) return "gf-chip-apagado";   // en curso o finalizada
        return estado == EstadoReserva.ListaEspera ? "gf-chip-espera" : "gf-chip-activo";
    }
}