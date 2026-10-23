using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;

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

    private static void Validar(Actividad actividad)
    {
        Validador.Obligatorio(actividad.NombreActividad, "nombre de la actividad");
        Validador.MayorQueCero(actividad.LimiteUsuarios, "límite de usuarios");
    }
}