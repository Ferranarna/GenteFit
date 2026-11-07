using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

// Página a la que va cualquier error no previsto (la configura Program.cs)
[AllowAnonymous]
public class ErrorController : Controller
{
    public IActionResult Index()
    {
        // Dirección donde ocurrió el error, para ofrecer "Reintentar"
        ViewBag.Ruta = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path;
        return View("Error");
    }
}