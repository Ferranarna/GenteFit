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

    // El cliente consulta el catálogo filtrando por grupo muscular
    public List<Ejercicio> BuscarPorGrupo(string grupo) =>
        _db.Ejercicios
            .Where(e => e.GrupoMuscular != null && e.GrupoMuscular.Contains(grupo))
            .OrderBy(e => e.NombreEjercicio)
            .ToList();
}
