using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;

namespace GenteFit.Tests.Datos;

public class IniciarSesionTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly UsuarioRepositorio _repo;

    public IniciarSesionTests()
    {
        _repo = new UsuarioRepositorio(_bd.Contexto);
    }

    public void Dispose() => _bd.Dispose();

    [Fact]
    public void Con_credenciales_correctas_devuelve_el_usuario()
    {
        var creado = DatosPrueba.CrearUsuario(_repo);

        var usuario = _repo.IniciarSesion("ana@prueba.com", DatosPrueba.Contrasena);

        Assert.Equal(creado.UsuarioId, usuario.UsuarioId);
    }

    [Fact]
    public void Con_contrasena_incorrecta_lanza_ValidacionException()
    {
        DatosPrueba.CrearUsuario(_repo);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.IniciarSesion("ana@prueba.com", "otra-contrasena"));

        Assert.Equal("Email o contraseña incorrectos.", error.Message);
    }

    [Fact]
    public void Con_email_que_no_existe_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(
            () => _repo.IniciarSesion("nadie@prueba.com", DatosPrueba.Contrasena));
    }

    [Fact]
    public void Con_usuario_dado_de_baja_lanza_ValidacionException()
    {
        var creado = DatosPrueba.CrearUsuario(_repo);
        _repo.DarDeBaja(creado.UsuarioId);

        Assert.Throws<ValidacionException>(
            () => _repo.IniciarSesion("ana@prueba.com", DatosPrueba.Contrasena));
    }
}