using GenteFit.Datos.Repositorios;
using GenteFit.Xml;

namespace GenteFit.Web.Models;

// Una fila del resumen: la sesión tal como llegó de Odoo y qué ha pasado con ella.
public record FilaImportacion(DateTime Fecha, string Actividad, string Sala, string Entrenador,
    AccionImportacion Accion, string? Detalle);

// Resultado de importar un lote de vuelta.
public class ImportacionViewModel
{
    public string IdLote { get; init; } = string.Empty;
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public List<FilaImportacion> Filas { get; init; } = [];

    public int Cuantas(AccionImportacion accion) => Filas.Count(f => f.Accion == accion);

    // Une cada sesión recibida con su resultado (van en el mismo orden)
    public static ImportacionViewModel Crear(LoteRecibido lote, List<SesionAplicada> resultados) => new()
    {
        IdLote = lote.IdLote,
        Desde = lote.Desde,
        Hasta = lote.Hasta,
        Filas = lote.Sesiones
            .Zip(resultados, (s, r) => new FilaImportacion(
                s.Fecha, s.Actividad.Nombre, s.Sala.Nombre, s.Entrenador.Nombre, r.Accion, r.Detalle))
            .ToList()
    };

    public static string Texto(AccionImportacion accion) => accion switch
    {
        AccionImportacion.SinCambios => "Sin cambios",
        _ => accion.ToString()
    };

    // El color acompaña al texto, nunca lo sustituye (accesibilidad)
    public static string ClaseChip(AccionImportacion accion) => accion switch
    {
        AccionImportacion.Creada or AccionImportacion.Modificada => "gf-chip-activo",
        AccionImportacion.Cancelada => "gf-chip-espera",
        AccionImportacion.Rechazada => "gf-chip-baja",
        _ => "gf-chip-apagado"
    };
}