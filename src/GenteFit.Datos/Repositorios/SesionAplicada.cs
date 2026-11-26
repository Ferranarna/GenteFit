namespace GenteFit.Datos.Repositorios;

// Qué ha pasado con cada sesión recibida de Odoo.
public enum AccionImportacion { Creada, Modificada, Cancelada, SinCambios, Rechazada }

// Resultado de aplicar una sesión del lote de vuelta. Detalle: motivo del rechazo u otra información.
public record SesionAplicada(Guid SesionId, DateTime Fecha, AccionImportacion Accion, string? Detalle);