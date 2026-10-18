using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;

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
}