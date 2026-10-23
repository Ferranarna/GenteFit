using System;

namespace GenteFit.Datos.Repositorios;

// Operaciones comunes a todas las tablas (CRUD básico).
public class Repositorio<T> where T : class
{
    protected readonly GenteFitContext _db;

    public Repositorio(GenteFitContext db)
    {
        _db = db;
    }

    public virtual List<T> ObtenerTodos() => _db.Set<T>().ToList();

    public virtual T? ObtenerPorId(Guid id) => _db.Set<T>().Find(id);

    public virtual T Crear(T entidad)
    {
        _db.Set<T>().Add(entidad);
        _db.SaveChanges();
        return entidad;
    }

    public virtual void Actualizar(T entidad)
    {
        _db.Set<T>().Update(entidad);
        _db.SaveChanges();
    }
}