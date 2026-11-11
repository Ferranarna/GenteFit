using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class SesionesDelDiaTests : IDisposable
{
    private static readonly DateTime Dia = new(2026, 11, 11);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SesionRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Actividad _pilates;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public SesionesDelDiaTests()
    {
        var db = _bd.Contexto;
        _repo = new SesionRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 3);
        _pilates = DatosAgenda.CrearActividad(db, "Pilates", plazas: 3);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 6, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion Crear(Actividad actividad, DateTime fecha, EstadoSesion estado = EstadoSesion.Programada) =>
        DatosAgenda.CrearSesion(_bd.Contexto, actividad, _sala, _laura, fecha, estado);

    private void Reservar(Sesion sesion, Usuario cliente, EstadoReserva estado, int minutosAntes)
    {
        _bd.Contexto.Reservas.Add(new Reserva
        {
            SesionId = sesion.SesionId,
            UsuarioId = cliente.UsuarioId,
            Estado = estado,
            FechaReserva = sesion.Fecha.AddMinutes(-minutosAntes)
        });
        _bd.Contexto.SaveChanges();
    }

    [Fact]
    public void Lista_solo_las_sesiones_programadas_del_dia_ordenadas_por_hora()
    {
        var tarde = Crear(_yoga, Dia.AddHours(18));
        var manana = Crear(_pilates, Dia.AddHours(10));
        Crear(_yoga, Dia.AddHours(12), EstadoSesion.Cancelada);
        Crear(_yoga, Dia.AddDays(1).AddHours(10));
        Crear(_yoga, Dia.AddDays(-1).AddHours(23));

        var lista = _repo.ListarDelDia(Dia);

        Assert.Equal([manana.SesionId, tarde.SesionId], lista.Select(s => s.Sesion.SesionId));
    }

    [Fact]
    public void Filtra_por_actividad()
    {
        Crear(_yoga, Dia.AddHours(10));
        var pilates = Crear(_pilates, Dia.AddHours(11));

        var lista = _repo.ListarDelDia(Dia, _pilates.ActividadId);

        Assert.Equal(pilates.SesionId, Assert.Single(lista).Sesion.SesionId);
    }

    [Fact]
    public void Cuenta_confirmadas_y_lista_de_espera_sin_contar_las_canceladas()
    {
        var sesion = Crear(_yoga, Dia.AddHours(10));
        Reservar(sesion, _clientes[0], EstadoReserva.Confirmada, 60);
        Reservar(sesion, _clientes[1], EstadoReserva.Confirmada, 50);
        Reservar(sesion, _clientes[2], EstadoReserva.Confirmada, 40);
        Reservar(sesion, _clientes[3], EstadoReserva.ListaEspera, 30);
        Reservar(sesion, _clientes[4], EstadoReserva.Cancelada, 20);

        var fila = _repo.ListarDelDia(Dia).Single();

        Assert.Equal(3, fila.Confirmadas);
        Assert.Equal(1, fila.EnEspera);
        Assert.True(fila.Completa);
    }

    [Fact]
    public void Indica_la_posicion_del_cliente_en_la_lista_de_espera()
    {
        var sesion = Crear(_yoga, Dia.AddHours(10));
        Reservar(sesion, _clientes[0], EstadoReserva.Confirmada, 90);
        Reservar(sesion, _clientes[1], EstadoReserva.Confirmada, 80);
        Reservar(sesion, _clientes[2], EstadoReserva.Confirmada, 70);
        Reservar(sesion, _clientes[3], EstadoReserva.ListaEspera, 60);
        Reservar(sesion, _clientes[4], EstadoReserva.ListaEspera, 50);

        var fila = _repo.ListarDelDia(Dia, usuarioId: _clientes[4].UsuarioId).Single();

        Assert.Equal(EstadoReserva.ListaEspera, fila.MiEstado);
        Assert.Equal(2, fila.MiPosicion);
    }

    [Fact]
    public void Una_reserva_cancelada_cuenta_como_sin_reserva()
    {
        var sesion = Crear(_yoga, Dia.AddHours(10));
        Reservar(sesion, _clientes[0], EstadoReserva.Cancelada, 60);

        var fila = _repo.ListarDelDia(Dia, usuarioId: _clientes[0].UsuarioId).Single();

        Assert.Null(fila.MiEstado);
    }

    [Fact]
    public void Dias_con_sesiones_marca_solo_los_del_mes_con_sesiones_programadas()
    {
        Crear(_yoga, new DateTime(2026, 11, 2, 10, 0, 0));
        Crear(_yoga, new DateTime(2026, 11, 2, 18, 0, 0));
        Crear(_pilates, new DateTime(2026, 11, 30, 20, 0, 0));
        Crear(_yoga, new DateTime(2026, 11, 15, 10, 0, 0), EstadoSesion.Cancelada);
        Crear(_yoga, new DateTime(2026, 10, 31, 10, 0, 0));
        Crear(_yoga, new DateTime(2026, 12, 1, 10, 0, 0));

        var dias = _repo.DiasConSesiones(2026, 11);

        Assert.Equal([2, 30], dias.Order());
    }
}
