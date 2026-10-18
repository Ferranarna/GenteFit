using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

public class EntrenadorRepositorio : Repositorio<Entrenador>
{
    public EntrenadorRepositorio(GenteFitContext db) : base(db) { }

    public override Entrenador Crear(Entrenador entrenador)
    {
        Validar(entrenador);
        entrenador.Activo = true;
        return base.Crear(entrenador);
    }

    public override void Actualizar(Entrenador entrenador)
    {
        Validar(entrenador);
        base.Actualizar(entrenador);
    }

    //no se borra, se desactiva
    public void DarDeBaja(Guid entrenadorId)
    {
        var entrenador = ObtenerPorId(entrenadorId) ?? throw new ValidacionException("El entrenador no existe.");
        entrenador.Activo = false;
        _db.SaveChanges();
    }

    public List<Entrenador> Buscar(string texto) =>
        _db.Entrenadores
            .Where(e => e.Nombre.Contains(texto) || e.Apellido1.Contains(texto))
            .OrderBy(e => e.Nombre)
            .ToList();

    public List<Entrenador> ObtenerActivosPorEspecialidad(Especialidad especialidad) =>
        _db.Entrenadores.Where(e => e.Activo && e.Especialidad == especialidad).ToList();

    private static void Validar(Entrenador entrenador)
    {
        Validador.Obligatorio(entrenador.Nombre, "nombre");
        Validador.Obligatorio(entrenador.Apellido1, "primer apellido");
        Validador.Telefono(entrenador.Telefono);
    }
}
