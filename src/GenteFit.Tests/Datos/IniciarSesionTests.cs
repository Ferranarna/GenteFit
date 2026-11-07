using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class IniciarSesionTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly UsuarioRepositorio _repo;

    // xUnit crea una instancia nueva de esta clase para CADA test:
    // así cada test tiene su propia base de datos vacía
    public IniciarSesionTests()
    {
        _repo = new UsuarioRepositorio(_bd.Contexto);
    }

    public void Dispose() => _bd.Dispose();

    // Crea un usuario válido en la base de pruebas
    private Usuario CrearUsuario()
    {
        return _repo.Crear(new Usuario
        {
            Nombre = "Ana",
            Apellido1 = "García",
            Dni = "11111111A",
            Email = "ana@prueba.com",
            Rol = Rol.Cliente,
            Telefono = 622000001
        }, "Secreta123");
    }

    [Fact]
    public void Con_credenciales_correctas_devuelve_el_usuario()
    {
        // Preparar
        var creado = CrearUsuario();

        // Ejecutar
        var usuario = _repo.IniciarSesion("ana@prueba.com", "Secreta123");

        // Comprobar
        Assert.Equal(creado.UsuarioId, usuario.UsuarioId);
    }

    [Fact]
    public void Con_contrasena_incorrecta_lanza_ValidacionException()
    {
        CrearUsuario();

        var error = Assert.Throws<ValidacionException>(
            () => _repo.IniciarSesion("ana@prueba.com", "otra-contrasena"));

        Assert.Equal("Email o contraseña incorrectos.", error.Message);
    }

    [Fact]
    public void Con_email_que_no_existe_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(
            () => _repo.IniciarSesion("nadie@prueba.com", "Secreta123"));
    }

    [Fact]
    public void Con_usuario_dado_de_baja_lanza_ValidacionException()
    {
        var creado = CrearUsuario();
        _repo.DarDeBaja(creado.UsuarioId);

        Assert.Throws<ValidacionException>(
            () => _repo.IniciarSesion("ana@prueba.com", "Secreta123"));
    }
}