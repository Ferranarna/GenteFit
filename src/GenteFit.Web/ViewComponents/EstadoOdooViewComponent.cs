using GenteFit.Web.Helpers;
using GenteFit.Web.Models;
using GenteFit.Xml;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.ViewComponents;

// Bloque "Estado de la integración + Historial" de la pantalla Odoo.
// Es un componente: tiene su propia lógica y se pinta desde la vista con Component.InvokeAsync.
public class EstadoOdooViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _http;
    private readonly string _carpetaLotes;
    private readonly string _urlOdoo;

    public EstadoOdooViewComponent(IHttpClientFactory http, IConfiguration config, IWebHostEnvironment entorno)
    {
        _http = http;
        _carpetaLotes = ConfigOdoo.CarpetaLotes(config, entorno);
        _urlOdoo = ConfigOdoo.Url(config);
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var modelo = new EstadoOdooViewModel
        {
            UrlOdoo = _urlOdoo,
            OdooDisponible = await OdooResponde(),
            Lotes = HistorialOdoo.listar(_carpetaLotes).ToList()
        };
        return View(modelo);
    }

    // ¿Contesta el servidor Odoo? Como mucho 2 segundos: si está caído, la pantalla no se queda colgada.
    private async Task<bool> OdooResponde()
    {
        try
        {
            var cliente = _http.CreateClient();
            cliente.Timeout = TimeSpan.FromSeconds(2);
            using var respuesta = await cliente.GetAsync(new Uri(new Uri(_urlOdoo), "/web/login"));
            return respuesta.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return false;
        }
    }
}