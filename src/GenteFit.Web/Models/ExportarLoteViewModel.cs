using System.ComponentModel.DataAnnotations;

namespace GenteFit.Web.Models;

// Parámetros que identifican el lote de sesiones que se envía a Odoo.
// Las reglas coinciden con el XSD de ida (odoo/xsd/lote_sesiones_ida.xsd).
public class ExportarLoteViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Indica el identificador del lote.")]
    [RegularExpression(@"^GF-\d{4}-\d{2}-\d{2}-\d{3}$",
        ErrorMessage = "El identificador debe tener el formato GF-AAAA-MM-DD-NNN (por ejemplo, GF-2026-11-26-001).")]
    public string IdLote { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige la fecha de inicio del periodo."), DataType(DataType.Date)]
    public DateTime? Desde { get; set; }

    [Required(ErrorMessage = "Elige la fecha final del periodo."), DataType(DataType.Date)]
    public DateTime? Hasta { get; set; }

    public bool IncluirCanceladas { get; set; } = true;

    // Valores que se proponen al abrir la pantalla: lote de hoy y los próximos 7 días.
    public static ExportarLoteViewModel PorDefecto(DateTime hoy) => new()
    {
        IdLote = $"GF-{hoy:yyyy-MM-dd}-001",
        Desde = hoy.Date,
        Hasta = hoy.Date.AddDays(6)
    };

    // Regla entre dos campos: los atributos no pueden comparar Desde con Hasta.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Desde is DateTime desde && Hasta is DateTime hasta && hasta < desde)
            yield return new ValidationResult(
                "La fecha final no puede ser anterior a la de inicio.", [nameof(Hasta)]);
    }
}