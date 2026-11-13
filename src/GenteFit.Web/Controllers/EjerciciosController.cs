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

// Catálogo de ejercicios: el encargado lo gestiona; cliente y entrenador solo lo consultan.
[Authorize(Roles = "Cliente,Entrenador,Encargado,Administrador")]
public class EjerciciosController : Controller
{
    private readonly GenteFitContext _db;

    public EjerciciosController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(string? buscar, string? grupo)
    {
        var modelo = new EjerciciosViewModel
        {
            Buscar = buscar,
            Grupo = string.IsNullOrWhiteSpace(grupo) ? null : grupo,
            PuedeEditar = User.IsInRole(nameof(Rol.Encargado))
        };

        try
        {
            var repo = new EjercicioRepositorio(_db);
            if (!string.IsNullOrWhiteSpace(buscar))
                modelo.Ejercicios = repo.Buscar(buscar.Trim());
            else if (modelo.Grupo != null)
                modelo.Ejercicios = repo.BuscarPorGrupo(modelo.Grupo);
            else
                modelo.Ejercicios = repo.ObtenerTodos().OrderBy(e => e.NombreEjercicio).ToList();
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View(modelo);
    }

    [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Crear() => View("Formulario", new EjercicioFormularioViewModel());

    [HttpGet, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Editar(Guid ejercicioId)
    {
        try
        {
            var e = new EjercicioRepositorio(_db).ObtenerPorId(ejercicioId);
            if (e == null)
            {
                TempData["Error"] = "El ejercicio no existe.";
                return RedirectToAction(nameof(Index));
            }

            return View("Formulario", new EjercicioFormularioViewModel
            {
                EjercicioId = e.EjercicioId,
                Nombre = e.NombreEjercicio,
                GrupoMuscular = e.GrupoMuscular,
                Descripcion = e.Descripcion
            });
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(Rol.Encargado))]
    public IActionResult Guardar(EjercicioFormularioViewModel modelo)
    {
        // Si falta algún dato obligatorio, se vuelve al formulario con los mensajes en rojo
        if (!ModelState.IsValid)
            return View("Formulario", modelo);

        var nombre = modelo.Nombre!.Trim();
        var descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim();

        try
        {
            var repo = new EjercicioRepositorio(_db);

            if (modelo.EsNuevo)
            {
                repo.Crear(new Ejercicio
                {
                    NombreEjercicio = nombre,
                    GrupoMuscular = modelo.GrupoMuscular,
                    Descripcion = descripcion
                });
                TempData["Ok"] = $"Ejercicio «{nombre}» creado.";
            }
            else
            {
                // Se carga el ejercicio de la BD y solo se cambian los campos del formulario (la imagen se conserva)
                var ejercicio = repo.ObtenerPorId(modelo.EjercicioId!.Value);
                if (ejercicio == null)
                {
                    TempData["Error"] = "El ejercicio no existe.";
                    return RedirectToAction(nameof(Index));
                }

                ejercicio.NombreEjercicio = nombre;
                ejercicio.GrupoMuscular = modelo.GrupoMuscular;
                ejercicio.Descripcion = descripcion;
                repo.Actualizar(ejercicio);
                TempData["Ok"] = $"Ejercicio «{nombre}» modificado.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidacionException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }

        return View("Formulario", modelo);
    }
}
