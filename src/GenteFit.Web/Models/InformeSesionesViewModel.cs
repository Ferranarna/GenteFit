using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

public class InformeSesionesViewModel
{
    // Opciones del filtro de estado. "Programadas" son las que aún no han terminado;
    // "Finalizadas" no es un estado guardado: sale de la hora de la sesión.
    public enum Filtro { Todos, Programadas, Finalizadas, Canceladas }

    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public Guid? ActividadId { get; set; }
    public Filtro Estado { get; set; }

    public List<Actividad> Actividades { get; set; } = [];

    public InformeSesiones? Informe { get; set; }

    public static readonly (Filtro Valor, string Texto)[] Estados =
    [
        (Filtro.Todos, "Todos"),
        (Filtro.Programadas, "Programadas"),
        (Filtro.Finalizadas, "Finalizadas"),
        (Filtro.Canceladas, "Canceladas")
    ];

    // El informe con solo las sesiones que cumplen el filtro (sus totales se recalculan solos)
    public static InformeSesiones Filtrar(InformeSesiones informe, Filtro filtro, DateTime ahora)
    {
        bool Terminada(SesionDelInforme f) => f.Sesion.Fecha.AddMinutes(f.Sesion.Duracion) <= ahora;

        var filas = informe.Filas.Where(f => filtro switch
        {
            Filtro.Programadas => f.Sesion.Estado == EstadoSesion.Programada && !Terminada(f),
            Filtro.Finalizadas => f.Sesion.Estado == EstadoSesion.Programada && Terminada(f),
            Filtro.Canceladas => f.Sesion.Estado == EstadoSesion.Cancelada,
            _ => true
        }).ToList();

        return new InformeSesiones(filas);
    }

    public static (string Texto, DateTime Desde, DateTime Hasta)[] Atajos(DateTime hoy)
    {
        var primeroDelMes = new DateTime(hoy.Year, hoy.Month, 1);
        return
        [
            ("Este mes", primeroDelMes, primeroDelMes.AddMonths(1).AddDays(-1)),
            ("Mes pasado", primeroDelMes.AddMonths(-1), primeroDelMes.AddDays(-1)),
            ("Últimos 30 días", hoy.AddDays(-29), hoy)
        ];
    }
}