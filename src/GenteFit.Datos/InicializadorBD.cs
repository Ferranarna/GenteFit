using System;
using GenteFit.Datos.Seguridad;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos;

public static class InicializadorBD
{
    // Contraseña de todos los usuarios de prueba
    public const string ContrasenaPrueba = "GenteFit123";

    public static void Inicializar(GenteFitContext db)
    {
        db.Database.Migrate();

        if (!db.Salas.Any())
            CargarDatosPrueba(db);
    }

    public static void CargarDatosPrueba(GenteFitContext db)
    {
        //Salas
        var salaGrande = new Sala { NombreSala = "Sala Grande", Ubicacion = Ubicacion.Planta1 };
        var salaCiclo = new Sala { NombreSala = "Sala Ciclo", Ubicacion = Ubicacion.Planta2 };
        db.Salas.AddRange(salaGrande, salaCiclo);

        // Actividades (límite de 16 por defecto)
        var yoga = new Actividad { NombreActividad = "Yoga", Descripcion = "Equilibrio y flexibilidad", Intensidad = Intensidad.Baja, Especialidad = Especialidad.Yoga };
        var spinning = new Actividad { NombreActividad = "Spinning", Descripcion = "Ciclo indoor", Intensidad = Intensidad.Alta, Especialidad = Especialidad.Spinning };
        var zumba = new Actividad { NombreActividad = "Zumba", Descripcion = "Baile y cardio", Intensidad = Intensidad.Media, Especialidad = Especialidad.Zumba };
        var bodypump = new Actividad { NombreActividad = "Body Pump", Descripcion = "Fuerza con barra", Intensidad = Intensidad.Alta, Especialidad = Especialidad.Bodypump };
        db.Actividades.AddRange(yoga, spinning, zumba, bodypump);

        // Entrenadores
        var laura = new Entrenador { Nombre = "Laura", Apellido1 = "Gómez", Telefono = 600111222, Especialidad = Especialidad.Yoga };
        var marc = new Entrenador { Nombre = "Marc", Apellido1 = "Puig", Telefono = 600222333, Especialidad = Especialidad.Spinning };
        var sara = new Entrenador { Nombre = "Sara", Apellido1 = "Ruiz", Telefono = 600333444, Especialidad = Especialidad.Zumba };
        var pau = new Entrenador { Nombre = "Pau", Apellido1 = "Vidal", Telefono = 600444555, Especialidad = Especialidad.Bodypump };
        var david = new Entrenador { Nombre = "David", Apellido1 = "López", Telefono = 600555666, Especialidad = Especialidad.EntrenadorPersonal };
        db.Entrenadores.AddRange(laura, marc, sara, pau, david);

        // Ejercicios
        db.Ejercicios.AddRange(
            new Ejercicio { NombreEjercicio = "Sentadilla", GrupoMuscular = "Piernas" },
            new Ejercicio { NombreEjercicio = "Zancada", GrupoMuscular = "Piernas" },
            new Ejercicio { NombreEjercicio = "Press banca", GrupoMuscular = "Pecho" },
            new Ejercicio { NombreEjercicio = "Flexiones", GrupoMuscular = "Pecho" },
            new Ejercicio { NombreEjercicio = "Dominadas", GrupoMuscular = "Espalda" },
            new Ejercicio { NombreEjercicio = "Plancha", GrupoMuscular = "Core" });

        // Usuarios: uno de cada rol + 3 clientes
        string hash = HashContrasena.Generar(ContrasenaPrueba);
        db.Usuarios.AddRange(
            NuevoUsuario("Admin", "GenteFit", "00000000A", "admin@gentefit.com", Rol.Administrador, 611000000, hash),
            NuevoUsuario("Rosa", "Martín", "00000001B", "recepcion@gentefit.com", Rol.Recepcionista, 611000001, hash),
            NuevoUsuario("Jordi", "Serra", "00000002C", "encargado@gentefit.com", Rol.Encargado, 611000002, hash),
            NuevoUsuario("Ana", "García", "11111111A", "ana@correo.com", Rol.Cliente, 622000001, hash),
            NuevoUsuario("Luis", "Pérez", "22222222B", "luis@correo.com", Rol.Cliente, 622000002, hash),
            NuevoUsuario("Marta", "Soler", "33333333C", "marta@correo.com", Rol.Cliente, 622000003, hash));

        // Sesiones de los próximos 3 días
        var hoy = DateTime.Today;
        for (int dia = 1; dia <= 3; dia++)
        {
            var fecha = hoy.AddDays(dia);
            db.Sesiones.AddRange(
                new Sesion { Actividad = yoga, Sala = salaGrande, Entrenador = laura, Fecha = fecha.AddHours(10), Duracion = 60 },
                new Sesion { Actividad = spinning, Sala = salaCiclo, Entrenador = marc, Fecha = fecha.AddHours(18), Duracion = 45 },
                new Sesion { Actividad = zumba, Sala = salaGrande, Entrenador = sara, Fecha = fecha.AddHours(19), Duracion = 60 },
                new Sesion { Actividad = bodypump, Sala = salaGrande, Entrenador = pau, Fecha = fecha.AddHours(20), Duracion = 60 });
        }

        db.SaveChanges();
    }

    private static Usuario NuevoUsuario(string nombre, string apellido, string dni, string email, Rol rol, int telefono, string hash) =>
        new Usuario
        {
            Nombre = nombre,
            Apellido1 = apellido,
            Dni = dni,
            Email = email,
            Rol = rol,
            Telefono = telefono,
            Contrasena = hash,
            Activo = true
        };
}