namespace GenteFit.Web.Helpers;

// Datos de una tarjeta de estadística del inicio.

public record Estadistica(
    string Icono,
    string Titulo,
    string Valor,
    string Detalle,
    string TextoEnlace,
    string Enlace = "#",
    int? Porcentaje = null,
    string? Resaltado = null);