using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests;

// Datos de ejemplo que comparten todos los tests
public static class DatosPrueba
{
    public const string Contrasena = "Secreta123";

    // Crea un usuario válido en la base de pruebas. Cada test cambia solo lo que necesite distinto.
    public static Usuario CrearUsuario(UsuarioRepositorio repo,
        string email = "ana@prueba.com", string dni = "11111111A",
        Rol rol = Rol.Cliente, int telefono = 622000001)
    {
        return repo.Crear(new Usuario
        {
            Nombre = "Ana",
            Apellido1 = "García",
            Dni = dni,
            Email = email,
            Rol = rol,
            Telefono = telefono
        }, Contrasena);
    }

    // Crea varios usuarios del mismo rol. El prefijo evita repetir emails y DNI entre grupos distintos.
    public static List<Usuario> CrearUsuarios(UsuarioRepositorio repo, int cuantos, Rol rol, string prefijo)
    {
        var usuarios = new List<Usuario>();
        for (int i = 1; i <= cuantos; i++)
            usuarios.Add(CrearUsuario(repo, email: $"{prefijo}{i}@prueba.com", dni: $"{prefijo}{i}", rol: rol));
        return usuarios;
    }
}