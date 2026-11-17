using System.Globalization;
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

// La misma pantalla para el cliente y para el entrenador: sus citas, anular, y reservar una nueva
// (el cliente con un entrenador personal; el entrenador personal para uno de sus clientes).
[Authorize(Roles = "Cliente,Entrenador")]
public class MisCitasController : Controller
{
    private static readonly CultureInfo Es = new("es-ES");
    private readonly GenteFitContext _db;

    public MisCitasController(GenteFitContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(EstadoCita? estado, DateTime? dia, Guid? persona, bool nueva)
    {
        // Un entrenador que no es personal no tiene citas
        if (EsEntrenador && !Permisos.EsEntrenadorPersonal(User))
            return RedirectToAction("Index", "Home");

        var modelo = new MisCitasViewModel
        {
            EsEntrenador = EsEntrenador,
            Estado = estado,
            Ahora = DateTime.Now,
            Dia = (dia ?? DateTime.Today).Date,
            PersonaId = persona,
            Nueva = nueva
        };
        try
        {
            var citas = new CitaRepositorio(_db);
            var lista = EsEntrenador
                ? citas.ListarDeEntrenador(UsuarioActual(), estado)
                : citas.ListarDeCliente(UsuarioActual(), estado);
            modelo.Citas = MisCitasViewModel.Ordenar(lista, modelo.Ahora);

            if (EsEntrenador)
            {
                // Solo un entrenador personal da citas; los demás (yoga, spinning...) no ven el botón de nueva cita
                modelo.PuedeReservar = new EntrenadorRepositorio(_db).ObtenerPorUsuario(UsuarioActual())?.Especialidad
                    == Especialidad.EntrenadorPersonal;
                if (modelo.PuedeReservar)
                    modelo.Personas = new UsuarioRepositorio(_db).ObtenerClientes()
                        .Where(u => u.Activo)
                        .Select(u => new PersonaCita(u.UsuarioId, Nombre(u))).ToList();
            }
            else
            {
                modelo.Personas = new EntrenadorRepositorio(_db)
                    .ObtenerActivosPorEspecialidad(Especialidad.EntrenadorPersonal)
                    .OrderBy(e => e.Usuario.Nombre)
                    .Select(e => new PersonaCita(e.EntrenadorId, Nombre(e.Usuario))).ToList();
                modelo.PuedeReservar = modelo.Personas.Count > 0;
            }

            if (modelo.PuedeReservar)
            {
                modelo.PersonaId ??= modelo.Personas.Count > 0 ? modelo.Personas[0].Id : null;
                // Los huecos son siempre los del entrenador: el propio, o el que el cliente ha elegido
                var entrenadorId = EsEntrenador ? UsuarioActual() : modelo.PersonaId;
                if (entrenadorId != null)
                    modelo.Libres = citas.BuscarDisponibilidad(entrenadorId.Value, modelo.Dia).ToHashSet();
            }
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            ViewBag.Error = Errores.ErrorBaseDeDatos;
        }
        return View(modelo);
    }

    // Reserva una cita: la persona elegida es un entrenador (si reserva el cliente) o un cliente (si reserva el entrenador).
    // Quién reserva sale de la cookie, nunca del formulario.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reservar(Guid personaId, DateTime dia, DateTime? hora)
    {
        var correcto = false;
        try
        {
            if (hora == null)
                throw new ValidacionException("Elige un hueco libre.");

            var citas = new CitaRepositorio(_db);
            Cita cita;
            var paraQuien = "";
            if (EsEntrenador)
            {
                var cliente = new UsuarioRepositorio(_db).ObtenerPorId(personaId);
                if (cliente == null || cliente.Rol != Rol.Cliente)
                    throw new ValidacionException("El cliente no existe.");

                cita = citas.Reservar(personaId, UsuarioActual(), hora.Value);
                paraQuien = $" para {Nombre(cliente)}";
            }
            else
            {
                cita = citas.Reservar(UsuarioActual(), personaId, hora.Value);
            }

            TempData["Ok"] = $"Cita reservada{paraQuien} el {cita.FechaActividad.ToString("dddd d 'de' MMMM 'a las' HH:mm", Es)}.";
            correcto = true;
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }

        // Si falla, la tarjeta sigue abierta con la misma persona y día para volver a intentarlo
        return RedirectToAction(nameof(Index), new { persona = personaId, dia = dia.ToString("yyyy-MM-dd"), nueva = !correcto });
    }

    // Los huecos libres de un día. La tarjeta "Nueva cita" los pide al cambiar de día o de entrenador, sin recargar la página.
    [HttpGet]
    public IActionResult Huecos(DateTime dia, Guid? persona)
    {
        try
        {
            var entrenadorId = EsEntrenador ? UsuarioActual() : persona;
            if (entrenadorId == null)
                return Json(Array.Empty<string>());

            var libres = new CitaRepositorio(_db).BuscarDisponibilidad(entrenadorId.Value, dia.Date);
            return Json(libres.Select(h => h.ToString("yyyy-MM-ddTHH:mm")));
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            return StatusCode(500);
        }
    }

    // Cada rol anula solo las suyas: el Id de quien anula sale de la cookie, nunca del formulario
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Anular(Guid citaId, EstadoCita? estado)
    {
        try
        {
            var citas = new CitaRepositorio(_db);
            var cita = EsEntrenador
                ? citas.AnularDeEntrenador(UsuarioActual(), citaId)
                : citas.AnularDeCliente(UsuarioActual(), citaId);
            TempData["Ok"] = $"Cita del {cita.FechaActividad.ToString("dddd d 'de' MMMM 'a las' HH:mm", Es)} anulada.";
        }
        catch (ValidacionException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (Errores.EsDeBaseDeDatos(ex))
        {
            TempData["Error"] = Errores.ErrorBaseDeDatos;
        }
        return RedirectToAction(nameof(Index), new { estado });
    }

    private bool EsEntrenador => User.IsInRole(nameof(Rol.Entrenador));

    private static string Nombre(Usuario u) => $"{u.Nombre} {u.Apellido1} {u.Apellido2}".Trim();

    // El Id del entrenador es el mismo que el de su usuario
    private Guid UsuarioActual() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}