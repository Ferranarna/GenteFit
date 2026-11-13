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
    // Informe: actividades más reservadas entre dos fechas (las dos incluidas).
    // Cuenta solo las sesiones programadas (las canceladas no tienen reservas válidas).
    public InformeActividades Informe(DateTime desde, DateTime hasta)
    {
        if (hasta < desde)
            throw new ValidacionException("La fecha «hasta» no puede ser anterior a «desde».");

        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var sesiones = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin)
            .Select(s => new { s.SesionId, s.ActividadId })
            .ToList();

        var ids = sesiones.Select(s => s.SesionId).ToList();
        var actividadDeSesion = sesiones.ToDictionary(s => s.SesionId, s => s.ActividadId);

        var reservas = _db.Reservas
            .Where(r => ids.Contains(r.SesionId) && r.Estado != EstadoReserva.Cancelada)
            .Select(r => new { r.SesionId, r.Estado })
            .ToList();

        var filas = _db.Actividades.ToList()
            .Select(a =>
            {
                var suyas = reservas.Where(r => actividadDeSesion[r.SesionId] == a.ActividadId).ToList();
                return new ActividadDelInforme(
                    a,
                    sesiones.Count(s => s.ActividadId == a.ActividadId),
                    suyas.Count(r => r.Estado == EstadoReserva.Confirmada),
                    suyas.Count(r => r.Estado == EstadoReserva.ListaEspera));
            })
            .OrderByDescending(f => f.Confirmadas)
            .ThenBy(f => f.Actividad.NombreActividad)
            .ToList();

        return new InformeActividades(filas);
    }

    private static void Validar(Actividad actividad)
    {
        Validador.Obligatorio(actividad.NombreActividad, "nombre de la actividad");
        Validador.MayorQueCero(actividad.LimiteUsuarios, "límite de usuarios");
    }
}