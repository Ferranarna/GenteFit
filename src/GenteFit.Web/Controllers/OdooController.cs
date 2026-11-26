using System.Text.RegularExpressions;
using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Enums;
using GenteFit.Web.Helpers;
using GenteFit.Web.Models;
using GenteFit.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Collections;

namespace GenteFit.Web.Controllers;

// Pantalla de intercambio con Odoo (Producto 4): exportar e importar lotes XML de sesiones.
[Authorize(Roles = nameof(Rol.Administrador))]
public class OdooController : Controller
{
    private readonly GenteFitContext _db;
    private readonly string _carpetaLotes;

    public OdooController(GenteFitContext db, IConfiguration config, IWebHostEnvironment entorno)
    {
        _db = db;
        // Carpeta donde se guarda una copia de cada lote. Si no se configura, "lotes" dentro de la web.
        _carpetaLotes = Path.Combine(entorno.ContentRootPath, config["Odoo:CarpetaLotes"] ?? "lotes");
    }

    [HttpGet]
    public IActionResult Index()
    {
        var modelo = ExportarLoteViewModel.PorDefecto(DateTime.Today);
        modelo.IdLote = SiguienteIdLote(DateTime.Today);
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Exportar(ExportarLoteViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), modelo);

        if (System.IO.File.Exists(RutaLote(modelo.IdLote)))
        {
            ModelState.AddModelError(nameof(modelo.IdLote),
                $"Ya existe el lote {modelo.IdLote}. Cambia el número final del identificador.");
            return View(nameof(Index), modelo);
        }

        try
        {
            var desde = modelo.Desde!.Value;
            var hasta = modelo.Hasta!.Value;

            var filas = new SesionRepositorio(_db).ListarParaExportar(desde, hasta, modelo.IncluirCanceladas);
            var sesiones = ListModule.OfSeq(filas.Select(MapeoOdoo.ASesionLote));
            var cabecera = new CabeceraLote(idLote: modelo.IdLote, generado: DateTime.Now, desde: desde, hasta: hasta);
            var rutaXsd = Path.Combine(EsquemasOdoo.carpeta, "lote_sesiones_ida.xsd");

            var resultado = ExportadorOdoo.generarLoteIda(rutaXsd, cabecera, sesiones);
            if (resultado.IsError)
            {
                foreach (var motivo in resultado.ErrorValue)
                    ModelState.AddModelError(string.Empty, motivo);
                return View(nameof(Index), modelo);
            }

            Directory.CreateDirectory(_carpetaLotes);
            System.IO.File.WriteAllBytes(RutaLote(modelo.IdLote), ExportadorOdoo.aBytes(resultado.ResultValue));

            TempData["LoteGenerado"] = modelo.IdLote;
            TempData["Info"] = $"Lote {modelo.IdLote} generado con {filas.Count} sesiones.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ViewBag.Error = $"No se ha podido guardar el lote en {_carpetaLotes}. Comprueba que la carpeta existe y que hay permiso de escritura.";
        }

        return View(nameof(Index), modelo);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5_000_000)]
    public IActionResult Importar(IFormFile? fichero)
    {
        if (fichero == null || fichero.Length == 0)
            return ErrorImportacion("Elige el fichero XML que has exportado desde Odoo.");
        if (!Path.GetExtension(fichero.FileName).Equals(".xml", StringComparison.OrdinalIgnoreCase))
            return ErrorImportacion("El fichero debe tener la extensión .xml.");

        // Se copia a memoria: se lee una vez para validarlo y otra para guardar la copia
        using var memoria = new MemoryStream();
        fichero.CopyTo(memoria);
        memoria.Position = 0;

        var rutaXsd = Path.Combine(EsquemasOdoo.carpeta, "lote_sesiones_vuelta.xsd");
        var lectura = ImportadorOdoo.leerLoteVuelta(rutaXsd, memoria);
        if (lectura.IsError)
            return ErrorImportacion(lectura.ErrorValue.ToArray());

        var lote = lectura.ResultValue;
        try
        {
            // Primero la copia: si no se puede guardar, no se aplica nada
            Directory.CreateDirectory(_carpetaLotes);
            System.IO.File.WriteAllBytes(RutaLote(lote.IdLote), memoria.ToArray());

            var resultados = new SesionRepositorio(_db)
                .AplicarLoteDesdeOdoo(lote.Sesiones.Select(MapeoOdoo.ASesion), DateTime.Now);

            return View("Importacion", ImportacionViewModel.Crear(lote, resultados));
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            return ErrorImportacion(Errores.ErrorBaseDeDatos);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ErrorImportacion($"No se ha podido guardar el lote en {_carpetaLotes}. Comprueba que la carpeta existe y que hay permiso de escritura.");
        }
    }

    // Vuelve a la pantalla principal mostrando por qué no se ha importado el lote
    private IActionResult ErrorImportacion(params string[] motivos)
    {
        ViewBag.ErroresImportacion = motivos;
        var modelo = ExportarLoteViewModel.PorDefecto(DateTime.Today);
        modelo.IdLote = SiguienteIdLote(DateTime.Today);
        return View(nameof(Index), modelo);
    }

    [HttpGet]
    public IActionResult Descargar(string id)
    {
        // Solo nombres de lote válidos: evita que se pida "../../appsettings.json"
        if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^(GF|OD)-\d{4}-\d{2}-\d{2}-\d{3}$"))
            return NotFound();

        var ruta = RutaLote(id);
        if (!System.IO.File.Exists(ruta))
            return NotFound();

        return PhysicalFile(ruta, "application/xml", $"{id}.xml");
    }

    private string RutaLote(string idLote) => Path.Combine(_carpetaLotes, $"{idLote}.xml");

    // Propone el primer número libre del día: GF-AAAA-MM-DD-001, -002...
    private string SiguienteIdLote(DateTime hoy)
    {
        for (var n = 1; n < 1000; n++)
        {
            var id = $"GF-{hoy:yyyy-MM-dd}-{n:000}";
            if (!System.IO.File.Exists(RutaLote(id)))
                return id;
        }
        return $"GF-{hoy:yyyy-MM-dd}-999";
    }
}