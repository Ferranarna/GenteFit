using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using System.Runtime.Intrinsics.X86;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GenteFit.Tests.Datos;

public class InformeSesionesTests : IDisposable
{
    private static readonly DateTime Desde = new(2026, 11, 1);
    private static readonly DateTime Hasta = new(2026, 11, 30);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SesionRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Actividad _pilates;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public InformeSesionesTests()
    {
        var db = _bd.Contexto;
        _repo = new SesionRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 4);
        _pilates = DatosAgenda.CrearActividad(db, "Pilates", plazas: 4);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 6, Rol.Cliente, "cli");
    }
    public void Dispose() => _bd.Dispose();

    private Sesion Crear(DateTime fecha, Actividad? actividad = null, EstadoSesion estado = EstadoSesion.Programada) =>
        DatosAgenda.CrearSesion(_bd.Contexto, actividad ?? _yoga, _sala, _laura, fecha, estado);

    private void Reservas(Sesion sesion, int cuantas, EstadoReserva estado, int saltar = 0) =>
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Skip(saltar).Take(cuantas), estado);

    [Fact]
    public void Incluye_desde_y_hasta_con_su_dia_entero()
    {
        Crear(new DateTime(2026, 10, 31, 23, 0, 0));                 // día antes: fuera
        var primera = Crear(new DateTime(2026, 11, 1, 9, 0, 0));     // primer día: dentro
        var ultima = Crear(new DateTime(2026, 11, 30, 23, 0, 0));    // último día a las 23:00: dentro
        Crear(new DateTime(2026, 12, 1, 9, 0, 0));                   // día después: fuera

        var informe = _repo.Informe(Desde, Hasta);

        Assert.Equal(new[] { primera.SesionId, ultima.SesionId }, informe.Filas.Select(f => f.Sesion.SesionId));
    }

    [Fact]
    public void Filtra_por_actividad_y_por_estado()
    {
        var yoga = Crear(Desde.AddHours(10));
        Crear(Desde.AddHours(12), _pilates);
        var cancelada = Crear(Desde.AddHours(18), estado: EstadoSesion.Cancelada);

        Assert.Equal(2, _repo.Informe(Desde, Hasta, actividadId: _yoga.ActividadId).Filas.Count);
        Assert.Equal(cancelada.SesionId,
            Assert.Single(_repo.Informe(Desde, Hasta, estado: EstadoSesion.Cancelada).Filas).Sesion.SesionId);
        Assert.Equal(yoga.SesionId,
            Assert.Single(_repo.Informe(Desde, Hasta, _yoga.ActividadId, EstadoSesion.Programada).Filas).Sesion.SesionId);
    }

    [Fact]
    public void Solo_cuenta_las_reservas_confirmadas()
    {
        var sesion = Crear(Desde.AddHours(10));
        Reservas(sesion, 2, EstadoReserva.Confirmada);
        Reservas(sesion, 1, EstadoReserva.ListaEspera, saltar: 2);
        Reservas(sesion, 1, EstadoReserva.Cancelada, saltar: 3);

        var fila = Assert.Single(_repo.Informe(Desde, Hasta).Filas);

        Assert.Equal(2, fila.Confirmadas);
        Assert.Equal(50, fila.Porcentaje);   // 2 de 4 plazas
    }

    [Fact]
    public void Calcula_los_totales_de_las_tarjetas()
    {
        var llena = Crear(Desde.AddHours(10));
        var floja = Crear(Desde.AddHours(12));
        Crear(Desde.AddHours(18), estado: EstadoSesion.Cancelada);
        Reservas(llena, 3, EstadoReserva.Confirmada);
        Reservas(floja, 1, EstadoReserva.Confirmada, saltar: 3);

        var informe = _repo.Informe(Desde, Hasta);

        Assert.Equal(2, informe.Programadas);
        Assert.Equal(1, informe.Canceladas);
        Assert.Equal(50, informe.OcupacionMedia);   // (3 + 1) de (4 + 4) plazas; la cancelada no cuenta
        Assert.Null(informe.Filas.Last().Porcentaje);
    }

    [Fact]
    public void Sin_sesiones_no_hay_ocupacion_media()
    {
        var informe = _repo.Informe(Desde, Hasta);

        Assert.Empty(informe.Filas);
        Assert.Null(informe.OcupacionMedia);
    }

    [Fact]
    public void Falla_si_hasta_es_anterior_a_desde()
    {
        var ex = Assert.Throws<ValidacionException>(() => _repo.Informe(Hasta, Desde));

        Assert.Equal("La fecha «hasta» no puede ser anterior a «desde».", ex.Message);
    }
}