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

public class ReservarTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly ReservaRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public ReservarTests()
    {
        var db = _bd.Contexto;
        _repo = new ReservaRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 3);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 4, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion CrearSesion(DateTime fecha) =>
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, fecha);

    [Fact]
    public void Con_plazas_libres_la_reserva_queda_confirmada()
    {
        var sesion = CrearSesion(DateTime.Now.AddDays(1));

        var reserva = _repo.Reservar(_clientes[0].UsuarioId, sesion.SesionId);

        Assert.Equal(EstadoReserva.Confirmada, reserva.Estado);
    }

    [Fact]
    public void Con_la_sesion_llena_el_cliente_queda_en_lista_de_espera()
    {
        var sesion = CrearSesion(DateTime.Now.AddDays(1));
        foreach (var cliente in _clientes.Take(3))
            _repo.Reservar(cliente.UsuarioId, sesion.SesionId);

        var reserva = _repo.Reservar(_clientes[3].UsuarioId, sesion.SesionId);

        Assert.Equal(EstadoReserva.ListaEspera, reserva.Estado);
        Assert.Equal(1, _repo.ObtenerPosicion(reserva.ReservaId));
    }

    [Fact]
    public void Una_sesion_que_ya_ha_empezado_lanza_ValidacionException()
    {
        var pasada = CrearSesion(DateTime.Now.AddHours(-1));

        var error = Assert.Throws<ValidacionException>(
            () => _repo.Reservar(_clientes[0].UsuarioId, pasada.SesionId));

        Assert.Equal("La sesión ya ha empezado.", error.Message);
    }
}
