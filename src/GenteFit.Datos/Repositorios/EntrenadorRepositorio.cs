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

    // Misma baja que cualquier usuario: comprueba sus sesiones y cancela sus citas futuras
    public void DarDeBaja(Guid entrenadorId) => new UsuarioRepositorio(_db).DarDeBaja(entrenadorId);

    public Entrenador? ObtenerPorUsuario(Guid usuarioId) =>
        _db.Entrenadores.Include(e => e.Usuario).FirstOrDefault(e => e.EntrenadorId == usuarioId);

    public List<Entrenador> Buscar(string texto) =>
        _db.Entrenadores
            .Include(e => e.Usuario)
            .Where(e => e.Usuario.Nombre.Contains(texto) || e.Usuario.Apellido1.Contains(texto)
                || e.Usuario.Email.Contains(texto))
            .OrderBy(e => e.Usuario.Nombre)
            .ToList();

    public List<Entrenador> ObtenerActivosPorEspecialidad(Especialidad especialidad) =>
        _db.Entrenadores
            .Include(e => e.Usuario)
            .Where(e => e.Usuario.Activo && e.Especialidad == especialidad)
            .ToList();

    public List<Entrenador> ObtenerActivos() =>
       _db.Entrenadores
           .Include(e => e.Usuario)
           .Where(e => e.Usuario.Activo)
           .OrderBy(e => e.Usuario.Nombre)
           .ToList();

    // Informe de carga: horas de sesiones (programadas) y de citas (no canceladas) de cada
    // entrenador activo entre dos fechas, las dos incluidas. De más a menos horas.
    public InformeEntrenadores InformeCarga(DateTime desde, DateTime hasta)
    {
        if (hasta < desde)
            throw new ValidacionException("La fecha «hasta» no puede ser anterior a «desde».");

        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var sesiones = _db.Sesiones
            .Where(s => s.Estado == EstadoSesion.Programada && s.Fecha >= inicio && s.Fecha < fin)
            .Select(s => new
            {
                s.EntrenadorId,
                s.Duracion,
                Plazas = s.Actividad.LimiteUsuarios,
                Confirmadas = s.Reservas.Count(r => r.Estado == EstadoReserva.Confirmada)
            })
            .ToList();

        var citas = _db.Citas
            .Where(c => c.Estado != EstadoCita.Cancelada && c.FechaActividad >= inicio && c.FechaActividad < fin)
            .Select(c => new { c.EntrenadorId, c.Duracion })
            .ToList();

        var filas = ObtenerActivos()
            .Select(e =>
            {
                var suyas = sesiones.Where(s => s.EntrenadorId == e.EntrenadorId).ToList();
                var susCitas = citas.Where(c => c.EntrenadorId == e.EntrenadorId).ToList();
                return new EntrenadorDelInforme(e,
                    suyas.Count, suyas.Sum(s => s.Duracion),
                    suyas.Sum(s => s.Confirmadas), suyas.Sum(s => s.Plazas),
                    susCitas.Count, susCitas.Sum(c => c.Duracion));
            })
            .OrderByDescending(f => f.Horas)
            .ThenBy(f => f.Entrenador.Usuario.Nombre)
            .ToList();

        return new InformeEntrenadores(filas);
    }
}
