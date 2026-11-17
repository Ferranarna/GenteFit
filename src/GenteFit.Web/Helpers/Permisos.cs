using System.Security.Claims;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Helpers;

// Quién puede gestionar las pantallas del centro (crear, modificar, dar de baja e informes).
// El administrador tiene acceso total, así que gestiona igual que el encargado.
public static class Permisos
{
    // Para los atributos [Authorize(Roles = ...)]
    public const string Gestion = nameof(Rol.Encargado) + "," + nameof(Rol.Administrador);

    // Dato que se guarda en la sesión al iniciarla: el entrenador es de la especialidad "entrenador personal"
    public const string ClaimEntrenadorPersonal = "EntrenadorPersonal";

    // Para decidir en el código si se enseñan los botones de edición
    public static bool PuedeGestionar(ClaimsPrincipal usuario) =>
        usuario.IsInRole(nameof(Rol.Encargado)) || usuario.IsInRole(nameof(Rol.Administrador));

    // Mis citas y Mis clientes son solo para el entrenador personal
    public static bool EsEntrenadorPersonal(ClaimsPrincipal usuario) =>
        usuario.HasClaim(ClaimEntrenadorPersonal, "true");
}