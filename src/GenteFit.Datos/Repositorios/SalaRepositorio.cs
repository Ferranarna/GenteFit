using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

public class SalaRepositorio : Repositorio<Sala>
{
    // Horario del centro (09:00 a 00:00): horas que una sala puede usarse cada día
    public const int HorasJornada = 15;

    public SalaRepositorio(GenteFitContext db) : base(db) { }

    public override Sala Crear(Sala sala)
    {
        Validar(sala);
        return base.Crear(sala);
    }

    public override void Actualizar(Sala sala)
    {
        Validar(sala);
        base.Actualizar(sala);
    }

    public List<Sala> Buscar(string texto) =>
        _db.Salas.Where(s => s.NombreSala.Contains(texto)).OrderBy(s => s.NombreSala).ToList();

    // Estadística: sala con más sesiones programadas en el mes (null si no hay ninguna)
    public (string Nombre, int Sesiones)? MasUsadaDelMes(DateTime fecha)
    {
        var inicio = new DateTime(fecha.Year, fecha.Month, 1);
        var fin = inicio.AddMonths(1);

        var mejor = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin)
            .GroupBy(s => s.Sala.NombreSala)
            .Select(g => new { Nombre = g.Key, Sesiones = g.Count() })
            .OrderByDescending(x => x.Sesiones).ThenBy(x => x.Nombre)
            .FirstOrDefault();

        return mejor == null ? null : (mejor.Nombre, mejor.Sesiones);
    }

    // Informe: uso de cada sala entre dos fechas (las dos incluidas).
    // Cuenta solo las sesiones programadas; las canceladas dejan la sala libre.
    public InformeSalas Informe(DateTime desde, DateTime hasta)
    {
        if (hasta < desde)
            throw new ValidacionException("La fecha «hasta» no puede ser anterior a «desde».");

        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);
        var dias = (fin - inicio).Days;

        var sesiones = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin)
            .Select(s => new { s.SesionId, s.SalaId, s.Duracion })
            .ToList();

        var ids = sesiones.Select(s => s.SesionId).ToList();
        var confirmadas = _db.Reservas
            .Where(r => ids.Contains(r.SesionId) && r.Estado == EstadoReserva.Confirmada)
            .Select(r => r.SesionId)
            .ToList();
        var salaDeSesion = sesiones.ToDictionary(s => s.SesionId, s => s.SalaId);

        var filas = _db.Salas.ToList()
            .Select(sala =>
            {
                var suyas = sesiones.Where(s => s.SalaId == sala.SalaId).ToList();
                return new SalaDelInforme(
                    sala,
                    suyas.Count,
                    suyas.Sum(s => s.Duracion),
                    confirmadas.Count(id => salaDeSesion[id] == sala.SalaId),
                    dias * HorasJornada * 60);
            })
            .OrderByDescending(f => f.Minutos)
            .ThenBy(f => f.Sala.NombreSala)
            .ToList();

        return new InformeSalas(filas, dias);
    }

    private void Validar(Sala sala)
    {
        Validador.Obligatorio(sala.NombreSala, "nombre de la sala");

        var nombre = sala.NombreSala.Trim();
        if (_db.Salas.Any(s => s.NombreSala == nombre && s.SalaId != sala.SalaId))
            throw new ValidacionException("Ya existe una sala con ese nombre.");
    }
}
