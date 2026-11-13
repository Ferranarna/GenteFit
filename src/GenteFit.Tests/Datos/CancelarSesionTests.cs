using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using GenteFit.Datos.Validaciones;

namespace GenteFit.Tests.Datos;

public class CancelarSesionTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SesionRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public CancelarSesionTests()
    {
        var db = _bd.Contexto;
        _repo = new SesionRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 3);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 6, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private static readonly DateTime Ahora = new(2026, 11, 11, 12, 0, 0);

    [Fact]
    public void Cancela_la_sesion_y_anula_reservas_confirmadas_y_en_espera()
    {
        var sesion = DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, Ahora.AddHours(6));
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Take(2), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Skip(2).Take(1), EstadoReserva.ListaEspera);

        var afectados = _repo.Cancelar(sesion.SesionId, Ahora);

        Assert.Equal(3, afectados.Count);
        using var leer = _bd.NuevoContexto();
        Assert.Equal(EstadoSesion.Cancelada, leer.Sesiones.Single().Estado);
        Assert.All(leer.Reservas, r => Assert.Equal(EstadoReserva.Cancelada, r.Estado));
    }

    [Fact]
    public void No_deja_cancelar_una_sesion_que_ya_ha_empezado()
    {
        var sesion = DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, Ahora);
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Take(2), EstadoReserva.Confirmada);

        var ex = Assert.Throws<ValidacionException>(() => _repo.Cancelar(sesion.SesionId, Ahora));

        Assert.Equal("No se puede cancelar una sesión que ya ha empezado.", ex.Message);
    }

    [Fact]
    public void No_deja_cancelar_una_sesion_ya_cancelada()
    {
        var sesion = DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, Ahora.AddHours(6), EstadoSesion.Cancelada);
        var ex = Assert.Throws<ValidacionException>(() => _repo.Cancelar(sesion.SesionId, Ahora));
        Assert.Equal("Esta sesión ya estaba cancelada.", ex.Message);
    }

    [Fact]
    public void Falla_si_la_sesion_no_existe()
    {
        var ex = Assert.Throws<ValidacionException>(() => _repo.Cancelar(Guid.NewGuid(), Ahora));
        Assert.Equal("La sesión no existe.", ex.Message);
    }

}
