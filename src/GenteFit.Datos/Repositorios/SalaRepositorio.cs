using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

public class SalaRepositorio : Repositorio<Sala>
{
    public SalaRepositorio(GenteFitContext db) : base(db) { }

    public override Sala Crear(Sala sala)
    {
        Validador.Obligatorio(sala.NombreSala, "nombre de la sala");
        return base.Crear(sala);
    }

    public override void Actualizar(Sala sala)
    {
        Validador.Obligatorio(sala.NombreSala, "nombre de la sala");
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
}