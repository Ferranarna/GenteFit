using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class EstadisticasUsuariosTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly UsuarioRepositorio _repo;

    public EstadisticasUsuariosTests()
    {
        _repo = new UsuarioRepositorio(_bd.Contexto);
    }

    public void Dispose() => _bd.Dispose();

    [Fact]
    public void ContarActivosYDeBaja_cuenta_cada_grupo()
    {
        DatosPrueba.CrearUsuarios(_repo, 3, Rol.Cliente, "act");
        var deBaja = DatosPrueba.CrearUsuarios(_repo, 1, Rol.Cliente, "baja");
        _repo.DarDeBaja(deBaja[0].UsuarioId);

        var (activos, bajas) = _repo.ContarActivosYDeBaja();

        Assert.Equal(3, activos);
        Assert.Equal(1, bajas);
    }

    [Fact]
    public void ContarActivosYDeBaja_con_la_base_vacia_da_cero_y_cero()
    {
        var (activos, bajas) = _repo.ContarActivosYDeBaja();

        Assert.Equal(0, activos);
        Assert.Equal(0, bajas);
    }

    [Fact]
    public void ContarClientesYPlantilla_separa_clientes_de_trabajadores()
    {
        DatosPrueba.CrearUsuarios(_repo, 2, Rol.Cliente, "cli");
        DatosPrueba.CrearUsuarios(_repo, 1, Rol.Recepcionista, "rec");
        DatosPrueba.CrearUsuarios(_repo, 1, Rol.Encargado, "enc");
        var deBaja = DatosPrueba.CrearUsuarios(_repo, 1, Rol.Cliente, "clibaja");
        _repo.DarDeBaja(deBaja[0].UsuarioId);

        var (clientes, plantilla) = _repo.ContarClientesYPlantilla();

        Assert.Equal(2, clientes);     // el cliente de baja no cuenta
        Assert.Equal(2, plantilla);    // recepcionista + encargado
    }

    [Fact]
    public void ContarClientesActivosYDeBaja_no_cuenta_a_los_trabajadores()
    {
        DatosPrueba.CrearUsuarios(_repo, 2, Rol.Cliente, "cli");
        DatosPrueba.CrearUsuarios(_repo, 1, Rol.Recepcionista, "rec");
        var deBaja = DatosPrueba.CrearUsuarios(_repo, 1, Rol.Cliente, "clibaja");
        _repo.DarDeBaja(deBaja[0].UsuarioId);

        var (activos, bajas) = _repo.ContarClientesActivosYDeBaja();

        Assert.Equal(2, activos);
        Assert.Equal(1, bajas);
    }
}