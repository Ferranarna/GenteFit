using System;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using Microsoft.EntityFrameworkCore;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

public class RutinaRepositorio : Repositorio<Rutina>
{
    public RutinaRepositorio(GenteFitContext db) : base(db) { }

    public Rutina Crear(Guid usuarioId, string nombre) => CrearRutina(usuarioId, nombre, null);

    public Rutina CrearParaCliente(Guid entrenadorId, Guid clienteId, string nombre)
    {
        var entrenador = _db.Entrenadores.Include(e => e.Usuario).FirstOrDefault(e => e.EntrenadorId == entrenadorId)
            ?? throw new ValidacionException("El entrenador no existe.");
        if (!entrenador.Usuario.Activo || entrenador.Especialidad != Especialidad.EntrenadorPersonal)
            throw new ValidacionException("Solo un entrenador personal activo puede crear rutinas para clientes.");

        if (!new CitaRepositorio(_db).EsClienteDe(entrenadorId, clienteId))
            throw new ValidacionException("El cliente no tiene ninguna cita con este entrenador.");

        return CrearRutina(clienteId, nombre, entrenadorId);
    }

    // La rutina guarda quién la creó (null si fue el propio cliente) y cuándo
    private Rutina CrearRutina(Guid usuarioId, string nombre, Guid? entrenadorId)
    {
        Validador.Obligatorio(nombre, "nombre de la rutina");
        return base.Crear(new Rutina
        {
            UsuarioId = usuarioId,
            NombreRutina = nombre,
            EntrenadorId = entrenadorId,
            FechaCreacion = DateTime.Now
        });
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

    // Todas las rutinas de un cliente: las que se hizo él y las que le han hecho sus entrenadores
    public List<Rutina> ObtenerDeCliente(Guid usuarioId) =>
        ConsultaConDetalle().Where(r => r.UsuarioId == usuarioId).OrderBy(r => r.NombreRutina).ToList();

    // Solo las rutinas que este entrenador le ha creado a ese cliente
    public List<Rutina> ObtenerDeEntrenador(Guid entrenadorId, Guid clienteId) =>
        ConsultaConDetalle()
            .Where(r => r.UsuarioId == clienteId && r.EntrenadorId == entrenadorId)
            .OrderBy(r => r.NombreRutina)
            .ToList();

    // Las rutinas con sus ejercicios y con quién las creó, para poder mostrarlas
    private IQueryable<Rutina> ConsultaConDetalle() =>
        _db.Rutinas
            .Include(r => r.Detalles).ThenInclude(d => d.Ejercicio)
            .Include(r => r.Usuario)
            .Include(r => r.Entrenador).ThenInclude(e => e!.Usuario);

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

    // El orden de los ejercicios de una rutina: el primero de la lista pasa a ser el 1, el segundo el 2...
    public void Ordenar(IList<Guid> detalleIds)
    {
        for (var i = 0; i < detalleIds.Count; i++)
        {
            var detalle = _db.DetallesRutina.Find(detalleIds[i]) ?? throw new ValidacionException("El ejercicio no está en la rutina.");
            detalle.Orden = i + 1;
        }
        _db.SaveChanges();
    }

    public void QuitarEjercicio(Guid detalleId)
    {
        var detalle = _db.DetallesRutina.Find(detalleId) ?? throw new ValidacionException("El ejercicio no está en la rutina.");
        _db.DetallesRutina.Remove(detalle);

        // Los que quedan se vuelven a numerar 1, 2, 3... para que no queden huecos en el orden
        var resto = _db.DetallesRutina
            .Where(d => d.RutinaId == detalle.RutinaId && d.DetalleRutinaId != detalleId)
            .OrderBy(d => d.Orden)
            .ToList();
        for (var i = 0; i < resto.Count; i++)
            resto[i].Orden = i + 1;

        _db.SaveChanges();
    }

    // Una rutina con sus ejercicios, solo si es de ese cliente (null si no existe o es de otro).
    // Así un cliente no puede ver la rutina de otro cambiando el id en la dirección.
    public Rutina? ObtenerDeCliente(Guid rutinaId, Guid usuarioId) =>
        _db.Rutinas
            .Include(r => r.Detalles).ThenInclude(d => d.Ejercicio)
            .FirstOrDefault(r => r.RutinaId == rutinaId && r.UsuarioId == usuarioId);

    // ¿La rutina es de este cliente y la creó ese entrenador? (entrenadorId null = la creó el propio cliente).
    // Es lo que decide quién puede modificarla: el cliente, las suyas; el entrenador, las que él creó.
    public bool EsDeCliente(Guid rutinaId, Guid usuarioId, Guid? entrenadorId = null) =>
        _db.Rutinas.Any(r => r.RutinaId == rutinaId && r.UsuarioId == usuarioId && r.EntrenadorId == entrenadorId);

    // Lo mismo para un ejercicio de rutina (detalle)
    public bool DetalleEsDeCliente(Guid detalleId, Guid usuarioId, Guid? entrenadorId = null) =>
        _db.DetallesRutina.Any(d => d.DetalleRutinaId == detalleId
            && d.Rutina.UsuarioId == usuarioId && d.Rutina.EntrenadorId == entrenadorId);

    // ¿La rutina es de este cliente, la haya creado quien la haya creado? (el cliente puede eliminar todas las suyas)
    public bool PerteneceACliente(Guid rutinaId, Guid usuarioId) =>
        _db.Rutinas.Any(r => r.RutinaId == rutinaId && r.UsuarioId == usuarioId);

    // Posición que le toca a un ejercicio nuevo: al final de la rutina
    public int SiguienteOrden(Guid rutinaId) =>
        (_db.DetallesRutina.Where(d => d.RutinaId == rutinaId).Max(d => (int?)d.Orden) ?? 0) + 1;

    // Límites razonables de una rutina (también los usa la pantalla Mis rutinas)
    public const int MaxSeries = 20;
    public const int MaxRepeticiones = 100;

    public static void ValidarConfiguracion(int series, int repeticiones)
    {
        Validador.MayorQueCero(series, "series");
        Validador.MayorQueCero(repeticiones, "repeticiones");
        if (series > MaxSeries)
            throw new ValidacionException($"Las series no pueden ser más de {MaxSeries}.");
        if (repeticiones > MaxRepeticiones)
            throw new ValidacionException($"Las repeticiones no pueden ser más de {MaxRepeticiones}.");
    }
}