﻿﻿﻿using System.Security.Claims;

namespace GenteFit.Web.Helpers;

// Pantallas de cada rol: de aquí salen las pestañas de arriba y las tarjetas del inicio.
public static class Menus
{
    private static readonly Dictionary<string, Tarjeta[]> PorRol = new()
    {
        ["Cliente"] =
        [
            new("actividad", "Actividades", "Conoce las actividades del centro.", "/Actividades"),
            new("mancuerna", "Ejercicios", "Consulta los ejercicios disponibles.", "/Ejercicios"),
            new("calendario", "Sesiones", "Consulta las sesiones disponibles.", "/Sesiones"),
            new("marcador", "Mis reservas", "Consulta tus reservas.", "/MisReservas"),
            new("lista", "Mis rutinas", "Personaliza tus rutinas de entrenamiento.", "/MisRutinas"),
            new("reloj", "Mis citas", "Organiza tus citas con tu entrenador.", "/MisCitas")
        ],
        ["Encargado"] =
        [
            new("actividad", "Actividades", "Gestiona las actividades del centro.", "/Actividades"),
            new("puerta", "Salas", "Gestiona las salas del centro.", "/Salas"),
            new("calendario", "Sesiones", "Gestiona las sesiones y sus reservas.", "/Sesiones"),
            new("entrenador", "Entrenadores", "Gestiona los entrenadores del centro.", "/Usuarios"),
            new("mancuerna", "Ejercicios", "Gestiona el catálogo de ejercicios.", "/Ejercicios")
        ],
        ["Recepcionista"] =
        [
            new("usuarios", "Clientes", "Gestiona los clientes del centro.", "/Usuarios"),
            new("actividad", "Actividades", "Consulta las actividades del centro.", "/Actividades"),
            new("calendario", "Sesiones", "Consulta las sesiones y su disponibilidad.", "/Sesiones")
        ],
        ["Administrador"] =
        [
             new("usuarios", "Usuarios", "Gestiona los usuarios y sus roles.", "/Usuarios"),
             new("intercambio", "Odoo", "Intercambia datos con el ERP Odoo.", "/Odoo")
        ],
        ["Entrenador"] =
        [
            new("calendario", "Mis sesiones", "Consulta tus sesiones y sus reservas.", "/Sesiones"),
            new("mancuerna", "Ejercicios", "Consulta los ejercicios disponibles.", "/Ejercicios"),
            new("reloj", "Mis citas", "Organiza tus citas con tus clientes.", "/MisCitas"),
            new("usuarios", "Mis clientes", "Consulta los clientes que entrenas.", "/MisClientes")
        ]
    };

    // Tarjetas del Centro de mando (Inicio del administrador): acceso a todas las pantallas.
    public static readonly Tarjeta[] CentroDeMando =
    [
        new("usuarios", "Usuarios", "Gestiona los usuarios y sus roles.", "/Usuarios"),
        new("usuario", "Clientes", "Gestiona los clientes del centro.", "/Usuarios?rol=Cliente"),
        new("actividad", "Actividades", "Gestiona las actividades del centro.", "/Actividades"),
        new("puerta", "Salas", "Gestiona las salas del centro.", "/Salas"),
        new("calendario", "Sesiones", "Gestiona las sesiones y sus reservas.", "/Sesiones"),
        new("entrenador", "Entrenadores", "Gestiona los entrenadores del centro.", "/Usuarios?rol=Entrenador"),
        new("mancuerna", "Ejercicios", "Gestiona el catálogo de ejercicios.", "/Ejercicios"),
        new("intercambio", "Odoo", "Intercambia datos con el ERP Odoo.", "/Odoo")
    ];

    // Pantallas del usuario que ha iniciado sesión (según su rol)
    public static IReadOnlyList<Tarjeta> De(ClaimsPrincipal usuario)
    {
        var rol = usuario.FindFirstValue(ClaimTypes.Role) ?? "";
        return PorRol.TryGetValue(rol, out var pantallas) ? pantallas : [];
    }
}