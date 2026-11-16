using System.Security.Claims;
using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using GenteFit.Web.Helpers;
using GenteFit.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenteFit.Web.Controllers;

// Gestión de usuarios: UNA pantalla para tres roles. Lo que se ve y se puede tocar depende de quién entra:
//   Recepcionista -> Clientes        (sin informe)
//   Encargado     -> Entrenadores    (informe de carga de entrenadores)
//   Administrador -> Usuarios        (todos los roles, cambia roles, informe de usuarios por rol)
[Authorize(Roles = "Recepcionista,Encargado,Administrador")]
public class UsuariosController : Controller
{
    private readonly GenteFitContext _db;

    public UsuariosController(GenteFitContext db)
    {
        _db = db;
    }

    // ---------- Quién gestiona qué ----------

    private Gestion MiGestion() => User.FindFirstValue(ClaimTypes.Role) switch
    {
        nameof(Rol.Recepcionista) => Gestion.Clientes,
        nameof(Rol.Encargado) => Gestion.Entrenadores,
        _ => Gestion.Usuarios
    };

    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ¿Puede el usuario conectado tocar a este usuario? (evita cambiar el id en la dirección)
    private bool PuedeGestionar(Usuario u) => MiGestion() switch
    {
        Gestion.Clientes => u.Rol == Rol.Cliente,
        Gestion.Entrenadores => u.Rol == Rol.Entrenador,
        _ => true
    };

    // ---------- Lista ----------

    [HttpGet]
    public IActionResult Index(string? buscar, Rol? rol)
    {
        var gestion = MiGestion();
        var modelo = new UsuariosViewModel
        {
            Gestion = gestion,
            Buscar = buscar,
            Rol = gestion == Gestion.Usuarios ? rol : null,
            UsuarioActual = UsuarioActual()
        };
        var texto = buscar?.Trim() ?? "";

        try
        {
            if (gestion == Gestion.Entrenadores)
            {
                modelo.Filas = new EntrenadorRepositorio(_db).Buscar(texto)
                    .Select(e => new FilaUsuario(e.Usuario, e.Especialidad))
                    .ToList();
            }
            else
            {
                var filtro = gestion == Gestion.Clientes ? Rol.Cliente : modelo.Rol;
                var usuarios = new UsuarioRepositorio(_db).Buscar(texto, filtro);
                var especialidades = _db.Entrenadores.ToDictionary(e => e.EntrenadorId, e => e.Especialidad);
                modelo.Filas = usuarios
                    .Select(u => new FilaUsuario(u,
                        especialidades.TryGetValue(u.UsuarioId, out var esp) ? esp : null))
                    .ToList();
            }
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }

    // ---------- Alta y modificación ----------

    [HttpGet]
    public IActionResult Crear()
    {
        var gestion = MiGestion();
        return View("Formulario", new UsuarioFormularioViewModel
        {
            Gestion = gestion,
            Rol = gestion == Gestion.Usuarios ? Rol.Cliente : null
        });
    }

    [HttpGet]
    public IActionResult Editar(Guid usuarioId)
    {
        try
        {
            var u = new UsuarioRepositorio(_db).ObtenerPorId(usuarioId);
            if (u == null || !PuedeGestionar(u))
            {
                TempData["Error"] = $"El {TextosUsuarios.Singular(MiGestion())} no existe.";
                return RedirectToAction(nameof(Index));
            }

            var entrenador = _db.Entrenadores.Find(u.UsuarioId);
            return View("Formulario", new UsuarioFormularioViewModel
            {
                UsuarioId = u.UsuarioId,
                Nombre = u.Nombre,
                Apellido1 = u.Apellido1,
                Apellido2 = u.Apellido2,
                Dni = u.Dni,
                Telefono = u.Telefono,
                Email = u.Email,
                Rol = u.Rol,
                Especialidad = entrenador?.Especialidad,
                Gestion = MiGestion()
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar(UsuarioFormularioViewModel modelo)
    {
        var gestion = MiGestion();
        modelo.Gestion = gestion;

        // El rol lo decide la pantalla, salvo para el administrador
        var rol = gestion switch
        {
            Gestion.Clientes => Rol.Cliente,
            Gestion.Entrenadores => Rol.Entrenador,
            _ => modelo.Rol ?? Rol.Cliente
        };
        modelo.Rol = rol;

        if (modelo.EsNueva && string.IsNullOrWhiteSpace(modelo.Contrasena))
            ModelState.AddModelError(nameof(modelo.Contrasena), "La contraseña inicial es obligatoria.");
        if (rol == Rol.Entrenador && modelo.Especialidad == null)
            ModelState.AddModelError(nameof(modelo.Especialidad), "Elige la especialidad.");

        if (!ModelState.IsValid)
            return View("Formulario", modelo);

        var datos = new Usuario
        {
            Nombre = modelo.Nombre!.Trim(),
            Apellido1 = modelo.Apellido1!.Trim(),
            Apellido2 = string.IsNullOrWhiteSpace(modelo.Apellido2) ? null : modelo.Apellido2.Trim(),
            Dni = modelo.Dni!.Trim().ToUpperInvariant(),
            Email = modelo.Email!.Trim(),
            Telefono = modelo.Telefono!.Value,
            Rol = rol
        };
        var nombre = TextosUsuarios.NombreCompleto(datos);
        var tipo = TextosUsuarios.Singular(gestion);

        try
        {
            if (modelo.EsNueva)
            {
                if (rol == Rol.Entrenador)
                    new EntrenadorRepositorio(_db).Crear(datos, modelo.Contrasena!, modelo.Especialidad!.Value);
                else
                    new UsuarioRepositorio(_db).Crear(datos, modelo.Contrasena!);
                TempData["Ok"] = $"{Mayuscula(tipo)} «{nombre}» dado de alta.";
            }
            else
            {
                var repo = new UsuarioRepositorio(_db);
                var actual = repo.ObtenerPorId(modelo.UsuarioId!.Value);
                if (actual == null || !PuedeGestionar(actual))
                {
                    TempData["Error"] = $"El {tipo} no existe.";
                    return RedirectToAction(nameof(Index));
                }

                // Nadie se quita a sí mismo el rol de administrador (se quedaría sin poder entrar aquí)
                if (actual.UsuarioId == UsuarioActual() && rol != actual.Rol)
                    throw new ValidacionException("No puedes cambiar tu propio rol.");

                repo.Modificar(actual.UsuarioId, datos, modelo.Especialidad);
                TempData["Ok"] = $"{Mayuscula(tipo)} «{nombre}» modificado.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidacionException ex)
        {
            // Por ejemplo: "Ya existe un usuario con ese DNI."
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View("Formulario", modelo);
    }

    // ---------- Baja y vuelta de alta ----------

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DarDeBaja(Guid usuarioId) =>
        CambiarEstado(usuarioId, baja: true);

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reactivar(Guid usuarioId) =>
        CambiarEstado(usuarioId, baja: false);

    private IActionResult CambiarEstado(Guid usuarioId, bool baja)
    {
        var tipo = TextosUsuarios.Singular(MiGestion());
        try
        {
            var repo = new UsuarioRepositorio(_db);
            var u = repo.ObtenerPorId(usuarioId);
            if (u == null || !PuedeGestionar(u))
                throw new ValidacionException($"El {tipo} no existe.");
            if (u.UsuarioId == UsuarioActual())
                throw new ValidacionException("No puedes darte de baja a ti mismo.");

            if (baja)
            {
                repo.DarDeBaja(usuarioId);
                TempData["Ok"] = $"«{TextosUsuarios.NombreCompleto(u)}» dado de baja. Se han anulado sus reservas y citas pendientes.";
            }
            else
            {
                repo.Reactivar(usuarioId);
                TempData["Ok"] = $"«{TextosUsuarios.NombreCompleto(u)}» vuelve a estar activo.";
            }
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------- Informes ----------

    // Encargado: carga de trabajo de los entrenadores. Por defecto, el mes actual.
    [HttpGet, Authorize(Roles = "Encargado,Administrador")]
    public IActionResult InformeEntrenadores(DateTime? desde, DateTime? hasta)
    {
        var hoy = DateTime.Today;
        var primeroDeMes = new DateTime(hoy.Year, hoy.Month, 1);
        var modelo = new InformeEntrenadoresViewModel
        {
            Desde = (desde ?? primeroDeMes).Date,
            Hasta = (hasta ?? primeroDeMes.AddMonths(1).AddDays(-1)).Date
        };

        try
        {
            modelo.Informe = new EntrenadorRepositorio(_db).InformeCarga(modelo.Desde, modelo.Hasta);
        }
        catch (ValidacionException ex)
        {
            ViewBag.Error = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }

    // Administrador: usuarios por rol
    [HttpGet, Authorize(Roles = "Administrador")]
    public IActionResult InformeRoles()
    {
        var filas = new List<RolDelInforme>();
        try
        {
            filas = new UsuarioRepositorio(_db).InformePorRol();
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View(filas);
    }

    // Botón de informe de la lista: cada rol va al suyo
    [HttpGet]
    public IActionResult Informe() => MiGestion() switch
    {
        Gestion.Entrenadores => RedirectToAction(nameof(InformeEntrenadores)),
        Gestion.Usuarios => RedirectToAction(nameof(InformeRoles)),
        _ => RedirectToAction(nameof(Index))
    };

    private static string Mayuscula(string texto) => char.ToUpper(texto[0]) + texto[1..];
}
