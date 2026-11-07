using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Tests.Datos;

public class ModificarPerfilTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly UsuarioRepositorio _repo;

    public ModificarPerfilTests()
    {
        _repo = new UsuarioRepositorio(_bd.Contexto);
    }

    public void Dispose() => _bd.Dispose();

    // Datos de perfil válidos; cada test cambia solo lo que necesita distinto
    private static Usuario Datos(string nombre = "Laura", string email = "laura@prueba.com", int telefono = 633000002) =>
        new Usuario { Nombre = nombre, Apellido1 = "Pérez", Apellido2 = "Ruiz", Email = email, Telefono = telefono };

    [Fact]
    public void Guarda_los_cambios_de_verdad_en_la_base_de_datos()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        _repo.ModificarPerfil(usuario.UsuarioId, Datos());

        // Se lee con OTRO contexto, para estar seguros de que se guardó y no solo está en memoria
        using var otro = _bd.NuevoContexto();
        var guardado = new UsuarioRepositorio(otro).ObtenerPorId(usuario.UsuarioId)!;
        Assert.Equal("Laura", guardado.Nombre);
        Assert.Equal("Pérez", guardado.Apellido1);
        Assert.Equal("Ruiz", guardado.Apellido2);
        Assert.Equal("laura@prueba.com", guardado.Email);
        Assert.Equal(633000002, guardado.Telefono);
    }

    [Fact]
    public void No_cambia_el_DNI_aunque_lleguen_datos_con_otro()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);
        var datos = Datos();
        datos.Dni = "99999999Z";

        _repo.ModificarPerfil(usuario.UsuarioId, datos);

        using var otro = _bd.NuevoContexto();
        var guardado = new UsuarioRepositorio(otro).ObtenerPorId(usuario.UsuarioId)!;
        Assert.Equal("11111111A", guardado.Dni);
    }

    [Fact]
    public void Puede_conservar_su_propio_email()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        // Cambia solo el nombre: el email es el suyo de siempre y no debe contar como repetido
        var resultado = _repo.ModificarPerfil(usuario.UsuarioId, Datos(email: "ana@prueba.com"));

        Assert.Equal("Laura", resultado.Nombre);
    }

    [Fact]
    public void Con_un_email_que_ya_usa_otro_usuario_lanza_ValidacionException()
    {
        DatosPrueba.CrearUsuario(_repo, email: "otro@prueba.com", dni: "22222222B");
        var usuario = DatosPrueba.CrearUsuario(_repo);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.ModificarPerfil(usuario.UsuarioId, Datos(email: "otro@prueba.com")));

        Assert.Equal("Ya existe un usuario con ese email.", error.Message);
    }

    [Fact]
    public void Con_un_telefono_incorrecto_lanza_ValidacionException()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        Assert.Throws<ValidacionException>(
            () => _repo.ModificarPerfil(usuario.UsuarioId, Datos(telefono: 12345)));
    }

    [Fact]
    public void Con_un_email_sin_formato_lanza_ValidacionException()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        Assert.Throws<ValidacionException>(
            () => _repo.ModificarPerfil(usuario.UsuarioId, Datos(email: "esto-no-es-un-email")));
    }

    [Fact]
    public void Con_el_nombre_vacio_lanza_ValidacionException()
    {
        var usuario = DatosPrueba.CrearUsuario(_repo);

        Assert.Throws<ValidacionException>(
            () => _repo.ModificarPerfil(usuario.UsuarioId, Datos(nombre: "")));
    }

    [Fact]
    public void Con_un_usuario_que_no_existe_lanza_ValidacionException()
    {
        var error = Assert.Throws<ValidacionException>(
            () => _repo.ModificarPerfil(Guid.NewGuid(), Datos()));

        Assert.Equal("El usuario no existe.", error.Message);
    }
}