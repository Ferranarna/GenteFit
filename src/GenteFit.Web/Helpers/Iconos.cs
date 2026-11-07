using Microsoft.AspNetCore.Html;

namespace GenteFit.Web.Helpers;

// Iconos de la web
public static class Iconos
{
    private static readonly Dictionary<string, string> Dibujos = new()
    {
        // Acciones de tabla y botones
        ["modificar"] = """<path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 013 3L7 19l-4 1 1-4z"/>""",
        ["baja"] = """<circle cx="12" cy="12" r="9"/><path d="M5.6 5.6l12.8 12.8"/>""",
        ["reservas"] = """<path d="M17 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2"/><circle cx="9.5" cy="7" r="4"/><path d="M22 21v-2a4 4 0 00-3-3.9M16 3.1a4 4 0 010 7.8"/>""",
        ["crear"] = """<path d="M12 5v14M5 12h14"/>""",
        ["quitar"] = """<path d="M5 12h14"/>""",
        ["informe"] = """<path d="M6 20V12M12 20V4M18 20V12M2 20h20"/>""",
        ["flecha"] = """<path d="M5 12h14M13 6l6 6-6 6"/>""",
        ["volver"] = """<path d="M19 12H5M11 6l-6 6 6 6"/>""",

        // Barra superior y menú de usuario
        ["usuario"] = """<circle cx="12" cy="8.5" r="4"/><path d="M4.5 20.5c.8-4 3.8-6 7.5-6s6.7 2 7.5 6"/>""",
        ["candado"] = """<rect x="5" y="10.5" width="14" height="10" rx="2.5"/><path d="M8 10.5V8a4 4 0 0 1 8 0v2.5"/><path d="M12 14.5v2.5"/>""",
        ["cerrar-sesion"] = """<path d="M9 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h3"/><path d="M16 8l4 4-4 4M20 12H9"/>""",
        ["menu"] = """<path d="M4 7h16M4 12h16M4 17h16"/>""",
        ["cerrar"] = """<path d="M6 6l12 12M18 6L6 18"/>""",
        ["sobre"] = """<rect x="3" y="5" width="18" height="14" rx="2.5"/><path d="M3.5 7l8.5 6 8.5-6"/>""",

        // Tarjetas de las pantallas de inicio
        ["actividad"] = """<path d="M2.5 12h4l2.5-7 5 14 2.5-7h5"/>""",
        ["puerta"] = """<path d="M5 21V4.5a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1V21"/><path d="M3 21h18"/><path d="M15 12h.01"/>""",
        ["calendario"] = """<rect x="3.5" y="5" width="17" height="15" rx="2.5"/><path d="M3.5 10h17M8 3v4M16 3v4"/>""",
        ["entrenador"] = """<circle cx="12" cy="8" r="4"/><path d="M4.5 20.5c.8-4 3.8-6 7.5-6s6.7 2 7.5 6"/><path d="M12 14.5v6"/>""",
        ["mancuerna"] = """<path d="M2.5 9.5v5M5.5 7v10M18.5 7v10M21.5 9.5v5M5.5 12h13"/>""",
        ["usuarios"] = """<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20c.6-3.6 3.1-5.5 6.5-5.5s5.9 1.9 6.5 5.5"/><circle cx="17.5" cy="9" r="2.5"/><path d="M17.5 14.5c2.3.1 3.7 1.6 4 4.5"/>""",
        ["casa"] = """<path d="M3 10.5L12 3l9 7.5"/><path d="M5 9.5V21h14V9.5"/><path d="M10 21v-6h4v6"/>""",
        ["marcador"] = """<path d="M6 3.5h12a1 1 0 0 1 1 1V21l-7-4-7 4V4.5a1 1 0 0 1 1-1z"/><path d="M9 10l2 2 4-4"/>""",
        ["lista"] = """<path d="M8 6h12M8 12h12M8 18h12"/><path d="M3.5 6h.01M3.5 12h.01M3.5 18h.01"/>""",
        ["reloj"] = """<circle cx="12" cy="12" r="8.5"/><path d="M12 7v5l3.5 2"/>"""
    };

    public static IHtmlContent Ver(string nombre)
    {
        Dibujos.TryGetValue(nombre, out var dibujo);
        return new HtmlString($"""<svg class="gf-ico" viewBox="0 0 24 24" aria-hidden="true">{dibujo}</svg>""");
    }
}