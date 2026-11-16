using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

public class InformeSesionesViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public Guid? ActividadId { get; set; }
    public EstadoSesion? Estado { get; set; }

    public List<Actividad> Actividades { get; set; } = [];

    public InformeSesiones? Informe { get; set; }

    public static readonly (EstadoSesion? Valor, string Texto)[] Estados =
    [
        (null, "Todos"),
        (EstadoSesion.Programada, "Programadas"),
        (EstadoSesion.Cancelada, "Canceladas")
    ];

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