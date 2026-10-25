using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos.Repositorios;

public class EntrenadorRepositorio : Repositorio<Entrenador>
{
    public EntrenadorRepositorio(GenteFitContext db) : base(db) { }

    public Entrenador Crear(Usuario usuario, string contrasena, Especialidad especialidad)
    {
        usuario.Rol = Rol.Entrenador;

        using var transaccion = _db.Database.BeginTransaction();
        var creado = new UsuarioRepositorio(_db).Crear(usuario, contrasena);
        var entrenador = base.Crear(new Entrenador { EntrenadorId = creado.UsuarioId, Especialidad = especialidad });
        transaccion.Commit();
        return entrenador;
    }

    public override Entrenador Crear(Entrenador entrenador)
    {
        var usuario = _db.Usuarios.Find(entrenador.EntrenadorId)
            ?? throw new ValidacionException("El usuario del entrenador no existe.");
        if (usuario.Rol != Rol.Entrenador)
            throw new ValidacionException("El usuario del entrenador debe tener el rol Entrenador.");
        if (_db.Entrenadores.Any(e => e.EntrenadorId == entrenador.EntrenadorId))
            throw new ValidacionException("Ese usuario ya es entrenador.");

        return base.Crear(entrenador);
    }

    public void DarDeBaja(Guid entrenadorId)
    {
        var entrenador = _db.Entrenadores.Include(e => e.Usuario).FirstOrDefault(e => e.EntrenadorId == entrenadorId)
            ?? throw new ValidacionException("El entrenador no existe.");
        entrenador.Usuario.Activo = false;
        _db.SaveChanges();
    }

    public Entrenador? ObtenerPorUsuario(Guid usuarioId) =>
        _db.Entrenadores.Include(e => e.Usuario).FirstOrDefault(e => e.EntrenadorId == usuarioId);

    public List<Entrenador> Buscar(string texto) =>
        _db.Entrenadores
            .Include(e => e.Usuario)
            .Where(e => e.Usuario.Nombre.Contains(texto) || e.Usuario.Apellido1.Contains(texto))
            .OrderBy(e => e.Usuario.Nombre)
            .ToList();

    public List<Entrenador> ObtenerActivosPorEspecialidad(Especialidad especialidad) =>
        _db.Entrenadores
            .Include(e => e.Usuario)
            .Where(e => e.Usuario.Activo && e.Especialidad == especialidad)
            .ToList();
}