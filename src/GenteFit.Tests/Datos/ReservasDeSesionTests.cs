using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class ReservasDeSesionTests : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 11, 11, 12, 0, 0);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly Actividad _yoga;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public ReservasDeSesionTests()
    {
        var db = _bd.Contexto;
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 3);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 6, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion CrearSesion(DateTime fecha) =>
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, fecha);

    // minutosAntes: cuánto antes de "Ahora" se hizo la reserva (más minutos = llegó antes)
    private Reserva Reservar(Sesion sesion, Usuario cliente, EstadoReserva estado, int minutosAntes) =>
        DatosAgenda.CrearReserva(_bd.Contexto, sesion, cliente, estado, Ahora.AddMinutes(-minutosAntes));

    [Fact]
    public void ObtenerConDetalle_trae_actividad_sala_y_entrenador()
    {
        var sesion = CrearSesion(Ahora.AddHours(6));

        using var db = _bd.NuevoContexto();
        var leido = new SesionRepositorio(db).ObtenerConDetalle(sesion.SesionId);

        Assert.NotNull(leido);
        Assert.Equal("Yoga", leido.Actividad.NombreActividad);
        Assert.Equal("Sala Grande", leido.Sala.NombreSala);
        Assert.Equal(_laura.EntrenadorId, leido.Entrenador.Usuario.UsuarioId);
    }

    [Fact]
    public void ObtenerConDetalle_devuelve_null_si_no_existe()
    {
        var repo = new SesionRepositorio(_bd.Contexto);

        Assert.Null(repo.ObtenerConDetalle(Guid.NewGuid()));
    }

    [Fact]
    public void La_lista_de_espera_sale_por_orden_de_llegada()
    {
        var sesion = CrearSesion(Ahora.AddHours(6));
        Reservar(sesion, _clientes[0], EstadoReserva.ListaEspera, minutosAntes: 10);   // llegó la última
        Reservar(sesion, _clientes[1], EstadoReserva.ListaEspera, minutosAntes: 30);   // llegó la primera
        Reservar(sesion, _clientes[2], EstadoReserva.ListaEspera, minutosAntes: 20);

        var espera = new ReservaRepositorio(_bd.Contexto).ListarListaEspera(sesion.SesionId);

        Assert.Equal(
            new[] { _clientes[1].UsuarioId, _clientes[2].UsuarioId, _clientes[0].UsuarioId },
            espera.Select(r => r.UsuarioId));
    }

    [Fact]
    public void Las_listas_no_incluyen_canceladas_ni_otras_sesiones()
    {
        var sesion = CrearSesion(Ahora.AddHours(6));
        var otra = CrearSesion(Ahora.AddHours(8));
        var confirmada = Reservar(sesion, _clientes[0], EstadoReserva.Confirmada, 30);
        var enEspera = Reservar(sesion, _clientes[1], EstadoReserva.ListaEspera, 20);
        Reservar(sesion, _clientes[2], EstadoReserva.Cancelada, 25);      // cancelada: no cuenta
        Reservar(otra, _clientes[3], EstadoReserva.Confirmada, 30);       // otra sesión: no cuenta
        Reservar(otra, _clientes[4], EstadoReserva.ListaEspera, 30);      // otra sesión: no cuenta

        var repo = new ReservaRepositorio(_bd.Contexto);

        Assert.Equal(confirmada.ReservaId, Assert.Single(repo.ListarConfirmados(sesion.SesionId)).ReservaId);
        Assert.Equal(enEspera.ReservaId, Assert.Single(repo.ListarListaEspera(sesion.SesionId)).ReservaId);
    }
}