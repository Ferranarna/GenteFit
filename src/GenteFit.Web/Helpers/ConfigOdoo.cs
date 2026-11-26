namespace GenteFit.Web.Helpers;

// Configuración del intercambio con Odoo (appsettings.json, sección "Odoo"). Un solo sitio para leerla.
public static class ConfigOdoo
{
    // Carpeta donde se guarda una copia de cada lote. Si no se configura, "lotes" dentro de la web.
    public static string CarpetaLotes(IConfiguration config, IWebHostEnvironment entorno) =>
        Path.Combine(entorno.ContentRootPath, config["Odoo:CarpetaLotes"] ?? "lotes");

    // Dirección del servidor Odoo. Por defecto, el de Docker en este mismo equipo.
    public static string Url(IConfiguration config) =>
        config["Odoo:Url"] ?? "http://localhost:8069";
}