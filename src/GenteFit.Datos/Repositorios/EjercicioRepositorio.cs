using System;

using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Datos.Repositorios;

public class EjercicioRepositorio : Repositorio<Ejercicio>
{
    public EjercicioRepositorio(GenteFitContext db) : base(db) { }

    public override Ejercicio Crear(Ejercicio ejercicio)
    {
        Validador.Obligatorio(ejercicio.NombreEjercicio, "nombre del ejercicio");
        return base.Crear(ejercicio);
    }

    public override void Actualizar(Ejercicio ejercicio)
    {
        Validador.Obligatorio(ejercicio.NombreEjercicio, "nombre del ejercicio");
        base.Actualizar(ejercicio);
    }

    // Catálogo ordenado por nombre. Se puede buscar por texto (nombre o grupo) y filtrar por grupo muscular, o las dos cosas.
    public List<Ejercicio> Buscar(string? texto, string? grupo = null)
    {
        var consulta = _db.Ejercicios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(texto))
            consulta = consulta.Where(e => e.NombreEjercicio.Contains(texto)
                || (e.GrupoMuscular != null && e.GrupoMuscular.Contains(texto)));

        if (!string.IsNullOrWhiteSpace(grupo))
            consulta = consulta.Where(e => e.GrupoMuscular != null && e.GrupoMuscular.Contains(grupo));

        return consulta.OrderBy(e => e.NombreEjercicio).ToList();
    }

    public List<Ejercicio> BuscarPorGrupo(string grupo) => Buscar(null, grupo);
}
