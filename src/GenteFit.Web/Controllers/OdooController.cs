using GenteFit.Modelo.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

// Pantalla de integración con Odoo (ERP). En el P3 solo es el diseño;
// la exportación/importación XML se implementa en el Producto 4.
[Authorize(Roles = nameof(Rol.Administrador))]
public class OdooController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}