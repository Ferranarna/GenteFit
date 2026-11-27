using GenteFit.Web.Helpers;
using GenteFit.Xml;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.ViewComponents;

// Tabla de lotes enviados y recibidos de la pantalla Odoo.
public class HistorialLotesViewComponent : ViewComponent
{
    private readonly string _carpetaLotes;

    public HistorialLotesViewComponent(IConfiguration config, IWebHostEnvironment entorno)
    {
        _carpetaLotes = ConfigOdoo.CarpetaLotes(config, entorno);
    }

    public IViewComponentResult Invoke() =>
        View(HistorialOdoo.listar(_carpetaLotes).ToList());
}