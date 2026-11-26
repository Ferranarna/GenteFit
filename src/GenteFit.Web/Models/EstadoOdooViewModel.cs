using GenteFit.Xml;

namespace GenteFit.Web.Models;

// Estado de la integración con Odoo e historial de lotes enviados y recibidos.
public class EstadoOdooViewModel
{
    public string UrlOdoo { get; init; } = string.Empty;
    public bool OdooDisponible { get; init; }
    public List<ResumenLote> Lotes { get; init; } = [];   // del más reciente al más antiguo

    public ResumenLote? UltimaExportacion => Lotes.FirstOrDefault(l => l.Enviado);
    public ResumenLote? UltimaImportacion => Lotes.FirstOrDefault(l => !l.Enviado);

    public int LotesEnviados => Lotes.Count(l => l.Enviado);
    public int LotesRecibidos => Lotes.Count(l => !l.Enviado);
    public int SesionesEnviadas => Lotes.Where(l => l.Enviado).Sum(l => l.Sesiones);
    public int SesionesRecibidas => Lotes.Where(l => !l.Enviado).Sum(l => l.Sesiones);
}