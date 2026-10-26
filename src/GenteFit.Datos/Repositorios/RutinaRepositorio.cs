using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using Microsoft.EntityFrameworkCore;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

public class RutinaRepositorio : Repositorio<Rutina>
{
    public RutinaRepositorio(GenteFitContext db) : base(db) { }

    public Rutina Crear(Guid usuarioId, string nombre)
    {
        Validador.Obligatorio(nombre, "nombre de la rutina");
        return base.Crear(new Rutina { UsuarioId = usuarioId, NombreRutina = nombre });
    }

    public Rutina CrearParaCliente(Guid entrenadorId, Guid clienteId, string nombre)
    {
        var entrenador = _db.Entrenadores.Include(e => e.Usuario).FirstOrDefault(e => e.EntrenadorId == entrenadorId)
            ?? throw new ValidacionException("El entrenador no existe.");
        if (!entrenador.Usuario.Activo || entrenador.Especialidad != Especialidad.EntrenadorPersonal)
            throw new ValidacionException("Solo un entrenador personal activo puede crear rutinas para clientes.");

        bool esSuCliente = _db.Citas.Any(c => c.EntrenadorId == entrenadorId
            && c.UsuarioId == clienteId
            && c.Estado == EstadoCita.Programada);
        if (!esSuCliente)
            throw new ValidacionException("El cliente no tiene ninguna cita con este entrenador.");

        return Crear(clienteId, nombre);
    }

    public void Renombrar(Guid rutinaId, string nombre)
    {
        Validador.Obligatorio(nombre, "nombre de la rutina");
        var rutina = ObtenerPorId(rutinaId) ?? throw new ValidacionException("La rutina no existe.");
        rutina.NombreRutina = nombre;
        _db.SaveChanges();
    }

    public void Eliminar(Guid rutinaId)
    {
        var rutina = ObtenerPorId(rutinaId) ?? throw new ValidacionException("La rutina no existe.");
        _db.Rutinas.Remove(rutina);
        _db.SaveChanges();
    }

    public List<Rutina> ObtenerDeCliente(Guid usuarioId) =>
        _db.Rutinas
            .Include(r => r.Detalles).ThenInclude(d => d.Ejercicio)
            .Where(r => r.UsuarioId == usuarioId)
            .OrderBy(r => r.NombreRutina)
            .ToList();

    public DetalleRutina AnadirEjercicio(Guid rutinaId, Guid ejercicioId, int orden, int series, int repeticiones)
    {
        ValidarConfiguracion(series, repeticiones);
        if (_db.Rutinas.Find(rutinaId) == null) throw new ValidacionException("La rutina no existe.");
        if (_db.Ejercicios.Find(ejercicioId) == null) throw new ValidacionException("El ejercicio no existe.");

        var detalle = new DetalleRutina
        {
            RutinaId = rutinaId,
            EjercicioId = ejercicioId,
            Orden = orden,
            Series = series,
            Repeticiones = repeticiones
        };
        _db.DetallesRutina.Add(detalle);
        _db.SaveChanges();
        return detalle;
    }

    public void ConfigurarEjercicio(Guid detalleId, int series, int repeticiones)
    {
        ValidarConfiguracion(series, repeticiones);
        var detalle = _db.DetallesRutina.Find(detalleId) ?? throw new ValidacionException("El ejercicio no está en la rutina.");
        detalle.Series = series;
        detalle.Repeticiones = repeticiones;
        _db.SaveChanges();
    }

    public void QuitarEjercicio(Guid detalleId)
    {
        var detalle = _db.DetallesRutina.Find(detalleId) ?? throw new ValidacionException("El ejercicio no está en la rutina.");
        _db.DetallesRutina.Remove(detalle);
        _db.SaveChanges();
    }

    private static void ValidarConfiguracion(int series, int repeticiones)
    {
        Validador.MayorQueCero(series, "series");
        Validador.MayorQueCero(repeticiones, "repeticiones");
    }
}