using System.Security.Claims;
using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GenteFit.Modelo.Entidades;

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

            await IniciarCookieAsync(usuario);

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
   
    [Authorize(Roles = "Cliente"), HttpGet]
    public IActionResult MisDatos()
    {
        ViewBag.Ok = TempData["Ok"];
        try
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var usuario = new UsuarioRepositorio(_db).ObtenerPorId(id);
            return View(usuario ?? new Usuario());
        }
        catch (Exception)
        {
            ViewBag.Error = "No se puede conectar con la base de datos. Inténtalo de nuevo más tarde.";
            return View(new Usuario());
        }
    }

    [Authorize(Roles = "Cliente"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MisDatos(string nombre, string apellido1, string? apellido2, string email, string telefono)
    {
        var datos = new Usuario
        {
            Nombre = nombre?.Trim() ?? "",
            Apellido1 = apellido1?.Trim() ?? "",
            Apellido2 = string.IsNullOrWhiteSpace(apellido2) ? null : apellido2.Trim(),
            Email = email?.Trim() ?? "",
            Telefono = int.TryParse(telefono, out var tel) ? tel : 0
        };

        try
        {
            var repo = new UsuarioRepositorio(_db);
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            datos.Dni = repo.ObtenerPorId(id)?.Dni ?? "";

            var guardado = repo.ModificarPerfil(id, datos);
            await IniciarCookieAsync(guardado);

            TempData["Ok"] = "Datos guardados correctamente.";
            return RedirectToAction(nameof(MisDatos));
        }
        catch (ValidacionException ex)
        {
            ViewBag.Error = ex.Message;
        }
        catch (Exception)
        {
            ViewBag.Error = "No se puede conectar con la base de datos. Inténtalo de nuevo más tarde.";
        }
        return View(datos);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // Crea (o vuelve a crear) la cookie de sesión con los datos actuales del usuario
    private async Task IniciarCookieAsync(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido1}"),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
    }
}