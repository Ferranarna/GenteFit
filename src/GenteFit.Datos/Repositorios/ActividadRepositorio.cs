using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

public class ActividadRepositorio : Repositorio<Actividad>
{
    public ActividadRepositorio(GenteFitContext db) : base(db) { }

    public override Actividad Crear(Actividad actividad)
    {
        Validar(actividad);
        return base.Crear(actividad);
    }

    public override void Actualizar(Actividad actividad)
    {
        Validar(actividad);
        base.Actualizar(actividad);
    }

    public List<Actividad> Buscar(string texto) =>
        _db.Actividades
            .Where(a => a.NombreActividad.Contains(texto) || (a.Descripcion != null && a.Descripcion.Contains(texto)))
            .OrderBy(a => a.NombreActividad)
            .ToList();

    // Estadística: actividad con más reservas confirmadas en el mes (null si no hay ninguna)
    public (string Nombre, int Reservas)? MasReservadaDelMes(DateTime fecha)
    {
        var inicio = new DateTime(fecha.Year, fecha.Month, 1);
        var fin = inicio.AddMonths(1);

        var mejor = _db.Reservas
            .Where(r => r.Estado == EstadoReserva.Confirmada && r.Sesion.Fecha >= inicio && r.Sesion.Fecha < fin)
            .GroupBy(r => r.Sesion.Actividad.NombreActividad)
            .Select(g => new { Nombre = g.Key, Reservas = g.Count() })
            .OrderByDescending(x => x.Reservas).ThenBy(x => x.Nombre)
            .FirstOrDefault();

        return mejor == null ? null : (mejor.Nombre, mejor.Reservas);
    }
    private static void Validar(Actividad actividad)
    {
        Validador.Obligatorio(actividad.NombreActividad, "nombre de la actividad");
        Validador.MayorQueCero(actividad.LimiteUsuarios, "límite de usuarios");
    }
}