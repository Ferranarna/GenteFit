using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class CitasTests : IDisposable
{
    private static readonly DateTime Manana10 = DateTime.Today.AddDays(1).AddHours(10);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly CitaRepositorio _repo;
    private readonly Entrenador _david;
    private readonly Entrenador _elena;
    private readonly List<Usuario> _clientes;

    public CitasTests()
    {
        var db = _bd.Contexto;
        _repo = new CitaRepositorio(db);
        _david = DatosAgenda.CrearEntrenador(db, "david", Especialidad.EntrenadorPersonal);
        _elena = DatosAgenda.CrearEntrenador(db, "elena", Especialidad.EntrenadorPersonal);
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 2, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    [Fact]
    public void Con_el_hueco_libre_la_cita_queda_programada()
    {
        var cita = _repo.Reservar(_clientes[0].UsuarioId, _david.EntrenadorId, Manana10);

        Assert.Equal(EstadoCita.Programada, cita.Estado);
        Assert.Equal(30, cita.Duracion);
    }

    [Fact]
    public void No_se_puede_reservar_una_hora_que_ya_ha_pasado()
    {
        var error = Assert.Throws<ValidacionException>(
            () => _repo.Reservar(_clientes[0].UsuarioId, _david.EntrenadorId, DateTime.Now.AddMinutes(-5)));

        Assert.Equal("Esa hora ya ha pasado.", error.Message);
    }

    [Fact]
    public void Un_cliente_dado_de_baja_no_puede_reservar()
    {
        _clientes[0].Activo = false;
        _bd.Contexto.SaveChanges();

        var error = Assert.Throws<ValidacionException>(
            () => _repo.Reservar(_clientes[0].UsuarioId, _david.EntrenadorId, Manana10));

        Assert.Equal("El cliente está dado de baja.", error.Message);
    }

    [Fact]
    public void Un_cliente_no_puede_tener_dos_citas_a_la_vez_aunque_sean_con_entrenadores_distintos()
    {
        _repo.Reservar(_clientes[0].UsuarioId, _david.EntrenadorId, Manana10);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.Reservar(_clientes[0].UsuarioId, _elena.EntrenadorId, Manana10.AddMinutes(15)));

        Assert.Equal("El cliente ya tiene una cita a esa hora.", error.Message);
    }

    [Fact]
    public void Dos_citas_seguidas_de_30_minutos_no_se_solapan()
    {
        _repo.Reservar(_clientes[0].UsuarioId, _david.EntrenadorId, Manana10);

        var segunda = _repo.Reservar(_clientes[0].UsuarioId, _elena.EntrenadorId, Manana10.AddMinutes(30));

        Assert.Equal(EstadoCita.Programada, segunda.Estado);
    }

    [Fact]
    public void Los_huecos_de_hoy_no_incluyen_horas_pasadas()
    {
        var huecos = _repo.BuscarDisponibilidad(_david.EntrenadorId, DateTime.Today);

        Assert.All(huecos, h => Assert.True(h > DateTime.Now));
    }

    [Fact]
    public void Un_hueco_ocupado_no_aparece_como_libre()
    {
        _repo.Reservar(_clientes[1].UsuarioId, _david.EntrenadorId, Manana10);

        var huecos = _repo.BuscarDisponibilidad(_david.EntrenadorId, Manana10.Date);

        Assert.DoesNotContain(Manana10, huecos);
        Assert.Contains(Manana10.AddMinutes(30), huecos);
    }

    private Cita Cita(Usuario cliente, Entrenador entrenador, DateTime fecha, EstadoCita estado = EstadoCita.Programada) =>
       DatosAgenda.CrearCita(_bd.Contexto, cliente, entrenador, fecha, estado);

    [Fact]
    public void Anular_una_cita_propia_y_futura_la_cancela()
    {
        var cita = Cita(_clientes[0], _david, Manana10);

        _repo.AnularDeCliente(_clientes[0].UsuarioId, cita.CitaId);

        using var db = _bd.NuevoContexto();
        Assert.Equal(EstadoCita.Cancelada, db.Citas.Find(cita.CitaId)!.Estado);
    }

    [Fact]
    public void No_se_puede_anular_la_cita_de_otro_cliente()
    {
        var deOtro = Cita(_clientes[1], _david, Manana10);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.AnularDeCliente(_clientes[0].UsuarioId, deOtro.CitaId));

        Assert.Equal("La cita no existe.", error.Message);
        using var db = _bd.NuevoContexto();
        Assert.Equal(EstadoCita.Programada, db.Citas.Find(deOtro.CitaId)!.Estado);
    }

    [Fact]
    public void No_se_puede_anular_una_cita_ya_cancelada()
    {
        var cancelada = Cita(_clientes[0], _david, Manana10, EstadoCita.Cancelada);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.AnularDeCliente(_clientes[0].UsuarioId, cancelada.CitaId));

        Assert.Equal("La cita ya está cancelada.", error.Message);
    }

    [Fact]
    public void No_se_puede_anular_una_cita_que_ya_ha_empezado()
    {
        var empezada = Cita(_clientes[0], _david, DateTime.Now.AddMinutes(-10));

        var error = Assert.Throws<ValidacionException>(
            () => _repo.AnularDeCliente(_clientes[0].UsuarioId, empezada.CitaId));

        Assert.Equal("La cita ya ha empezado.", error.Message);
    }

    [Fact]
    public void El_entrenador_solo_puede_anular_sus_propias_citas()
    {
        var deElena = Cita(_clientes[0], _elena, Manana10);

        var error = Assert.Throws<ValidacionException>(
            () => _repo.AnularDeEntrenador(_david.EntrenadorId, deElena.CitaId));
        _repo.AnularDeEntrenador(_elena.EntrenadorId, deElena.CitaId);

        Assert.Equal("La cita no existe.", error.Message);
        using var db = _bd.NuevoContexto();
        Assert.Equal(EstadoCita.Cancelada, db.Citas.Find(deElena.CitaId)!.Estado);
    }

    [Fact]
    public void Las_proximas_citas_del_cliente_no_incluyen_pasadas_ni_canceladas_ni_de_otros()
    {
        var proxima = Cita(_clientes[0], _david, Manana10);
        Cita(_clientes[0], _david, DateTime.Now.AddDays(-1));
        Cita(_clientes[0], _elena, Manana10.AddHours(2), EstadoCita.Cancelada);
        Cita(_clientes[1], _david, Manana10.AddHours(1));

        var lista = _repo.ListarProximasDeCliente(_clientes[0].UsuarioId);

        Assert.Equal(proxima.CitaId, Assert.Single(lista).CitaId);
        Assert.NotNull(lista[0].Entrenador.Usuario);
    }

    [Fact]
    public void Las_citas_del_entrenador_incluyen_pasadas_y_canceladas_y_se_filtran_por_estado()
    {
        Cita(_clientes[0], _david, DateTime.Now.AddDays(-1));
        Cita(_clientes[1], _david, Manana10, EstadoCita.Cancelada);
        Cita(_clientes[0], _elena, Manana10);

        Assert.Equal(2, _repo.ListarDeEntrenador(_david.EntrenadorId).Count);
        Assert.Single(_repo.ListarDeEntrenador(_david.EntrenadorId, EstadoCita.Cancelada));
    }

    [Fact]
    public void Mis_clientes_salen_una_vez_con_su_proxima_cita()
    {
        Cita(_clientes[0], _david, Manana10.AddDays(2));
        Cita(_clientes[0], _david, Manana10);                    // la más próxima
        Cita(_clientes[0], _david, DateTime.Now.AddDays(-3));    // pasada: no es "próxima"
        Cita(_clientes[1], _david, DateTime.Now.AddDays(-1));    // solo tiene una pasada
        Cita(_clientes[1], _elena, Manana10);                    // con otra entrenadora: no cuenta

        var clientes = _repo.ListarClientesDeEntrenador(_david.EntrenadorId);

        Assert.Equal(2, clientes.Count);
        Assert.Equal(Manana10, clientes.Single(c => c.Cliente.UsuarioId == _clientes[0].UsuarioId).ProximaCita);
        Assert.Null(clientes.Single(c => c.Cliente.UsuarioId == _clientes[1].UsuarioId).ProximaCita);
    }
}

