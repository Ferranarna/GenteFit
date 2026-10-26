using System.Security.Claims;
using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GenteFit.Web.Controllers;

public class CuentaController : Controller
{
    private readonly GenteFitContext _db;

    public CuentaController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string contrasena)
    {
        ViewBag.Email = email;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(contrasena))
        {
            ViewBag.Error = "Introduce tu email y tu contraseña.";
            return View();
        }

        try
        {
            var usuario = new UsuarioRepositorio(_db).IniciarSesion(email.Trim(), contrasena);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido1}"),
                new(ClaimTypes.Role, usuario.Rol.ToString())
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

            return RedirectToAction("Index", "Home");
        }
        catch (ValidacionException ex)
        {
            ViewBag.Error = ex.Message;
            return View();
        }
        catch (Exception)
        {
            ViewBag.Error = "No se puede conectar con la base de datos. Comprueba que SQL Server está en marcha y que la conexión es correcta.";
            return View();
        }
    }

    [Authorize, HttpGet]
    public IActionResult CambiarContrasena() => View();

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public IActionResult CambiarContrasena(string actual, string nueva)
    {
        try
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            new UsuarioRepositorio(_db).CambiarContrasena(id, actual ?? "", nueva ?? "");
            ViewBag.Ok = "Contraseña cambiada correctamente.";
        }
        catch (ValidacionException ex)
        {
            ViewBag.Error = ex.Message;
        }
        catch (Exception)
        {
            ViewBag.Error = "No se puede conectar con la base de datos. Inténtalo de nuevo más tarde.";
        }
        return View();
    }
    [HttpPost, ValidateAntiForgeryToken]

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}