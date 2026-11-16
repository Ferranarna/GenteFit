using System.Security.Claims;
using GenteFit.Modelo.Enums;

namespace GenteFit.Web.Helpers;

// Quién puede gestionar las pantallas del centro (crear, modificar, dar de baja e informes).
// El administrador tiene acceso total (requisito del P1), así que gestiona igual que el encargado.
public static class Permisos
{
    // Para los atributos [Authorize(Roles = ...)]
    public const string Gestion = nameof(Rol.Encargado) + "," + nameof(Rol.Administrador);

    // Para decidir en el código si se enseñan los botones de edición
    public static bool PuedeGestionar(ClaimsPrincipal usuario) =>
        usuario.IsInRole(nameof(Rol.Encargado)) || usuario.IsInRole(nameof(Rol.Administrador));
}