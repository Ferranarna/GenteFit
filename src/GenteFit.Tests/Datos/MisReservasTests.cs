using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenteFit.Tests.Datos;

public class MisReservasTests : IDisposable
{
    private static readonly DateTime Lunes = new(2026, 11, 16, 10, 0, 0);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly ReservaRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public MisReservasTests()
    {
        var db = _bd.Contexto;
        _repo = new ReservaRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 2);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 5, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion Sesion(DateTime fecha) => DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, fecha);

    private Reserva Reserva(Sesion sesion, Usuario cliente, EstadoReserva estado, int minutosAntes) =>
        DatosAgenda.CrearReserva(_bd.Contexto, sesion, cliente, estado, sesion.Fecha.AddMinutes(-minutosAntes));

    [Fact]
    public void Lista_todas_las_del_cliente_incluidas_las_canceladas_ordenadas_por_fecha_de_sesion()
    {
        var ana = _clientes[0];
        var miercoles = Reserva(Sesion(Lunes.AddDays(2)), ana, EstadoReserva.Confirmada, 60);
        var lunes = Reserva(Sesion(Lunes), ana, EstadoReserva.Cancelada, 60);
        Reserva(Sesion(Lunes.AddDays(1)), _clientes[1], EstadoReserva.Confirmada, 60);   // de otro cliente

        var lista = _repo.ListarDeCliente(ana.UsuarioId);

        Assert.Equal([lunes.ReservaId, miercoles.ReservaId], lista.Select(m => m.Reserva.ReservaId));
    }

    [Fact]
    public void Trae_la_sesion_con_su_actividad_y_su_sala_para_mostrarlas()
    {
        Reserva(Sesion(Lunes), _clientes[0], EstadoReserva.Confirmada, 60);

        var reserva = _repo.ListarDeCliente(_clientes[0].UsuarioId).Single().Reserva;

        Assert.Equal("Yoga", reserva.Sesion.Actividad.NombreActividad);
        Assert.Equal("Sala Grande", reserva.Sesion.Sala.NombreSala);
    }

    [Fact]
    public void Filtra_por_estado()
    {
        var ana = _clientes[0];
        Reserva(Sesion(Lunes), ana, EstadoReserva.Confirmada, 60);
        var cancelada = Reserva(Sesion(Lunes.AddDays(1)), ana, EstadoReserva.Cancelada, 60);

        var lista = _repo.ListarDeCliente(ana.UsuarioId, EstadoReserva.Cancelada);

        Assert.Equal(cancelada.ReservaId, Assert.Single(lista).Reserva.ReservaId);
    }

    [Fact]
    public void Las_de_lista_de_espera_llevan_su_posicion_y_las_demas_no()
    {
        var sesion = Sesion(Lunes);
        Reserva(sesion, _clientes[1], EstadoReserva.Confirmada, 90);
        Reserva(sesion, _clientes[2], EstadoReserva.Confirmada, 80);
        Reserva(sesion, _clientes[3], EstadoReserva.ListaEspera, 70);
        Reserva(sesion, _clientes[0], EstadoReserva.ListaEspera, 60);   // Ana, segunda en la cola
        Reserva(Sesion(Lunes.AddDays(1)), _clientes[0], EstadoReserva.Confirmada, 60);

        var lista = _repo.ListarDeCliente(_clientes[0].UsuarioId);

        Assert.Equal(2, lista[0].Posicion);
        Assert.Null(lista[1].Posicion);
    }

    [Fact]
    public void La_posicion_solo_cuenta_la_cola_de_su_propia_sesion()
    {
        var lunes = Sesion(Lunes);
        var martes = Sesion(Lunes.AddDays(1));
        Reserva(martes, _clientes[1], EstadoReserva.ListaEspera, 3000);   // espera desde mucho antes, pero en otra sesión
        Reserva(lunes, _clientes[0], EstadoReserva.ListaEspera, 60);

        var miReserva = _repo.ListarDeCliente(_clientes[0].UsuarioId).Single();

        Assert.Equal(1, miReserva.Posicion);
    }

    [Fact]
    public void Un_cliente_sin_reservas_obtiene_una_lista_vacia()
    {
        Assert.Empty(_repo.ListarDeCliente(_clientes[4].UsuarioId));
    }

    [Fact]
    public void Anular_una_reserva_confirmada_pasa_al_primero_de_la_lista_de_espera()
    {
        var sesion = Sesion(DateTime.Now.AddDays(1));
        var ana = Reserva(sesion, _clientes[0], EstadoReserva.Confirmada, 90);
        Reserva(sesion, _clientes[1], EstadoReserva.Confirmada, 80);
        var primera = Reserva(sesion, _clientes[2], EstadoReserva.ListaEspera, 70);
        Reserva(sesion, _clientes[3], EstadoReserva.ListaEspera, 60);

        var promovida = _repo.AnularDeCliente(_clientes[0].UsuarioId, ana.ReservaId);

        Assert.Equal(primera.ReservaId, promovida?.ReservaId);
        using var db = _bd.NuevoContexto();
        Assert.Equal(EstadoReserva.Cancelada, db.Reservas.Find(ana.ReservaId)!.Estado);
        Assert.Equal(EstadoReserva.Confirmada, db.Reservas.Find(primera.ReservaId)!.Estado);
    }

    [Fact]
    public void No_se_puede_anular_la_reserva_de_otro_cliente()
    {
        var deLuis = Reserva(Sesion(DateTime.Now.AddDays(1)), _clientes[1], EstadoReserva.Confirmada, 60);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.AnularDeCliente(_clientes[0].UsuarioId, deLuis.ReservaId));

        Assert.Equal("La reserva no existe.", error.Message);
        using var db = _bd.NuevoContexto();
        Assert.Equal(EstadoReserva.Confirmada, db.Reservas.Find(deLuis.ReservaId)!.Estado);
    }

    [Fact]
    public void No_se_puede_anular_una_reserva_ya_cancelada()
    {
        var cancelada = Reserva(Sesion(DateTime.Now.AddDays(1)), _clientes[0], EstadoReserva.Cancelada, 60);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.AnularDeCliente(_clientes[0].UsuarioId, cancelada.ReservaId));

        Assert.Equal("La reserva ya está cancelada.", error.Message);
    }

    [Fact]
    public void No_se_puede_anular_si_la_sesion_ya_ha_empezado()
    {
        var empezada = Reserva(Sesion(DateTime.Now.AddMinutes(-10)), _clientes[0], EstadoReserva.Confirmada, 60);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.AnularDeCliente(_clientes[0].UsuarioId, empezada.ReservaId));

        Assert.Equal("La sesión ya ha empezado.", error.Message);
    }
}