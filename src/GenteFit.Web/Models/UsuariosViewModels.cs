using System.ComponentModel.DataAnnotations;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Models;

// La misma pantalla sirve para tres roles. Cada uno gestiona un grupo de usuarios:
// recepcionista -> clientes, encargado -> entrenadores, administrador -> todos.
public enum Gestion { Clientes, Entrenadores, Usuarios }

public static class TextosUsuarios
{
    public static string Titulo(Gestion g) => g.ToString();

    // "cliente", "entrenador" o "usuario", para los mensajes
    public static string Singular(Gestion g) => g switch
    {
        Gestion.Clientes => "cliente",
        Gestion.Entrenadores => "entrenador",
        _ => "usuario"
    };

    public static string Buscador(Gestion g) => g switch
    {
        Gestion.Clientes => "Buscar por nombre, DNI o email",
        Gestion.Entrenadores => "Buscar entrenador por nombre o email",
        _ => "Buscar por nombre, DNI o email"
    };

    public static string NombreCompleto(Usuario u) =>
        string.Join(" ", new[] { u.Nombre, u.Apellido1, u.Apellido2 }.Where(t => !string.IsNullOrWhiteSpace(t)));
}

// Una fila de la tabla. Especialidad solo la tienen los entrenadores.
public record FilaUsuario(Usuario Usuario, Especialidad? Especialidad);

// Pantalla de la lista (tabla con buscador)
public class UsuariosViewModel
{
    public Gestion Gestion { get; set; }
    public string? Buscar { get; set; }
    public Rol? Rol { get; set; }               // filtro por rol, solo el administrador
    public Guid UsuarioActual { get; set; }      // nadie se puede dar de baja a sí mismo
    public List<FilaUsuario> Filas { get; set; } = [];

    // Recepcionista no tiene informe; encargado (carga de entrenadores) y administrador (por rol) sí
    public bool TieneInforme => Gestion != Gestion.Clientes;
}

// Formulario de alta / modificación (el mismo para los tres roles)
public class UsuarioFormularioViewModel
{
    public Guid? UsuarioId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Apellido1 { get; set; }

    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Apellido2 { get; set; }

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\s*[0-9XYZxyz][0-9]{7}[A-Za-z]\s*$", ErrorMessage = "El DNI debe tener 8 cifras y una letra (o NIE).")]
    public string? Dni { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Range(100_000_000, 999_999_999, ErrorMessage = "El teléfono debe tener 9 cifras.")]
    public int? Telefono { get; set; }

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string? Email { get; set; }

    // Solo en el alta. Después cada usuario la cambia desde su menú.
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string? Contrasena { get; set; }

    // Solo lo elige el administrador. Para el resto lo pone el controlador.
    public Rol? Rol { get; set; }

    // Solo para entrenadores
    public Especialidad? Especialidad { get; set; }

    public Gestion Gestion { get; set; }   // lo rellena el controlador, no viene del formulario

    public bool EsNueva => UsuarioId == null;
}

// Informe del encargado
public class InformeEntrenadoresViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public InformeEntrenadores? Informe { get; set; }
}
