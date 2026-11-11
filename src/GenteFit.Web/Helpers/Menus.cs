using System.Security.Claims;

namespace GenteFit.Web.Helpers;

// Pantallas de cada rol: de aquí salen las pestañas de arriba y las tarjetas del inicio.
public static class Menus
{
    private static readonly Dictionary<string, Tarjeta[]> PorRol = new()
    {
        ["Cliente"] =
        [
            new("actividad", "Actividades", "Conoce las actividades del centro."),
            new("mancuerna", "Ejercicios", "Consulta los ejercicios disponibles."),
            new("calendario", "Sesiones", "Consulta las sesiones disponibles.", "/Sesiones"),
           new("marcador", "Mis reservas", "Consulta tus reservas.", "/MisReservas"),
            new("lista", "Mis rutinas", "Personaliza tus rutinas de entrenamiento."),
            new("reloj", "Mis citas", "Organiza tus citas con tu entrenador.")
        ],
        ["Encargado"] =
        [
            new("actividad", "Actividades", "Gestiona las actividades del centro."),
            new("puerta", "Salas", "Gestiona las salas del centro."),
            new("calendario", "Sesiones", "Consulta las sesiones disponibles.", "/Sesiones"),
            new("entrenador", "Entrenadores", "Gestiona los entrenadores del centro."),
            new("mancuerna", "Ejercicios", "Gestiona el catálogo de ejercicios.")
        ],
        ["Recepcionista"] =
        [
            new("usuarios", "Clientes", "Gestiona los clientes del centro."),
            new("actividad", "Actividades", "Consulta las actividades del centro."),
            new("calendario", "Sesiones", "Consulta las sesiones disponibles.", "/Sesiones")
        ],
        ["Administrador"] =
        [
            new("usuarios", "Usuarios", "Gestiona los usuarios y sus roles."),
            new("casa", "Centro de mando", "Accede a todas las funciones del centro.")
        ],
        ["Entrenador"] =
        [
            new("calendario", "Mis sesiones", "Consulta tus sesiones y sus reservas."),
            new("mancuerna", "Ejercicios", "Consulta los ejercicios disponibles."),
            new("reloj", "Mis citas", "Organiza tus citas con tus clientes."),
            new("usuarios", "Mis clientes", "Consulta los clientes que entrenas.")
        ]
    };

    // Pantallas del usuario que ha iniciado sesión (según su rol)
    public static IReadOnlyList<Tarjeta> De(ClaimsPrincipal usuario)
    {
        var rol = usuario.FindFirstValue(ClaimTypes.Role) ?? "";
        return PorRol.TryGetValue(rol, out var pantallas) ? pantallas : [];
    }
}