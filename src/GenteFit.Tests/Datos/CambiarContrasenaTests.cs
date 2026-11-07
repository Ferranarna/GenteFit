using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;

namespace GenteFit.Tests.Datos;

public class CambiarContrasenaTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly UsuarioRepositorio _repo;

    public CambiarContrasenaTests()
    {
        _repo = new UsuarioRepositorio(_bd.Contexto);
    }

    public void Dispose() => _bd.Dispose();

    [Fact]
    public void Con_la_actual_correcta_permite_entrar_con_la_nueva()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        _repo.CambiarContrasena(usuario.UsuarioId, DatosPrueba.Contrasena, "NuevaClave456");

        var entra = _repo.IniciarSesion("ana@prueba.com", "NuevaClave456");
        Assert.Equal(usuario.UsuarioId, entra.UsuarioId);
    }

    [Fact]
    public void Tras_cambiarla_la_contrasena_antigua_ya_no_vale()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        _repo.CambiarContrasena(usuario.UsuarioId, DatosPrueba.Contrasena, "NuevaClave456");

        Assert.Throws<ValidacionException>(
            () => _repo.IniciarSesion("ana@prueba.com", DatosPrueba.Contrasena));
    }

    [Fact]
    public void Con_la_actual_incorrecta_lanza_error_y_no_la_cambia()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.CambiarContrasena(usuario.UsuarioId, "equivocada", "NuevaClave456"));

        Assert.Equal("La contraseña actual no es correcta.", error.Message);
        // La contraseña de siempre sigue funcionando
        var entra = _repo.IniciarSesion("ana@prueba.com", DatosPrueba.Contrasena);
        Assert.Equal(usuario.UsuarioId, entra.UsuarioId);
    }

    [Fact]
    public void Con_la_nueva_vacia_lanza_ValidacionException()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        Assert.Throws<ValidacionException>(
            () => _repo.CambiarContrasena(usuario.UsuarioId, DatosPrueba.Contrasena, ""));
    }

    [Fact]
    public void Con_un_usuario_que_no_existe_lanza_ValidacionException()
    {
        var error = Assert.Throws<ValidacionException>(
            () => _repo.CambiarContrasena(Guid.NewGuid(), "cualquiera", "NuevaClave456"));

        Assert.Equal("El usuario no existe.", error.Message);
    }
}