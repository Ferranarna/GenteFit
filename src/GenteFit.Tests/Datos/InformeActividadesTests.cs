using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class InformeActividadesTests : IDisposable
{
    private static readonly DateTime Desde = new(2026, 11, 1);
    private static readonly DateTime Hasta = new(2026, 11, 30);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly ActividadRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Actividad _pilates;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public InformeActividadesTests()
    {
        var db = _bd.Contexto;
        _repo = new ActividadRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 4);
        _pilates = DatosAgenda.CrearActividad(db, "Pilates", plazas: 4);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 6, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion Crear(DateTime fecha, Actividad actividad, EstadoSesion estado = EstadoSesion.Programada) =>
        DatosAgenda.CrearSesion(_bd.Contexto, actividad, _sala, _laura, fecha, estado);

    private void Reservas(Sesion sesion, int cuantas, EstadoReserva estado, int saltar = 0) =>
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Skip(saltar).Take(cuantas), estado);

    [Fact]
    public void Ordena_de_mas_a_menos_reservas_confirmadas()
    {
        Reservas(Crear(Desde.AddHours(10), _yoga), 1, EstadoReserva.Confirmada);
        Reservas(Crear(Desde.AddHours(12), _pilates), 3, EstadoReserva.Confirmada);

        var informe = _repo.Informe(Desde, Hasta);

        Assert.Equal(new[] { "Pilates", "Yoga" }, informe.Filas.Select(f => f.Actividad.NombreActividad));
        Assert.Equal("Pilates", informe.MasReservada!.Actividad.NombreActividad);
        Assert.Equal(4, informe.TotalReservas);
    }

    [Fact]
    public void Separa_confirmadas_y_lista_de_espera_y_no_cuenta_las_anuladas()
    {
        var sesion = Crear(Desde.AddHours(10), _yoga);
        Reservas(sesion, 2, EstadoReserva.Confirmada);
        Reservas(sesion, 1, EstadoReserva.ListaEspera, saltar: 2);
        Reservas(sesion, 1, EstadoReserva.Cancelada, saltar: 3);

        var yoga = _repo.Informe(Desde, Hasta).Filas.Single(f => f.Actividad.ActividadId == _yoga.ActividadId);

        Assert.Equal(1, yoga.Sesiones);
        Assert.Equal(2, yoga.Confirmadas);
        Assert.Equal(1, yoga.EnEspera);
        Assert.Equal(4, yoga.Plazas);
        Assert.Equal(50, yoga.Porcentaje);
    }

    [Fact]
    public void No_cuenta_sesiones_canceladas_ni_fuera_del_periodo()
    {
        Reservas(Crear(Desde.AddHours(10), _yoga, EstadoSesion.Cancelada), 2, EstadoReserva.Confirmada);
        Reservas(Crear(new DateTime(2026, 10, 31, 23, 0, 0), _yoga), 2, EstadoReserva.Confirmada);
        Reservas(Crear(new DateTime(2026, 12, 1, 9, 0, 0), _yoga), 2, EstadoReserva.Confirmada);
        var ultimoDia = Crear(new DateTime(2026, 11, 30, 23, 0, 0), _yoga);   // último día a las 23:00: dentro
        Reservas(ultimoDia, 1, EstadoReserva.Confirmada);

        var yoga = _repo.Informe(Desde, Hasta).Filas.Single(f => f.Actividad.ActividadId == _yoga.ActividadId);

        Assert.Equal(1, yoga.Sesiones);
        Assert.Equal(1, yoga.Confirmadas);
    }

    [Fact]
    public void Incluye_las_actividades_sin_sesiones_al_final()
    {
        Reservas(Crear(Desde.AddHours(10), _yoga), 1, EstadoReserva.Confirmada);

        var informe = _repo.Informe(Desde, Hasta);
        var pilates = informe.Filas.Last();

        Assert.Equal("Pilates", pilates.Actividad.NombreActividad);
        Assert.Equal(0, pilates.Sesiones);
        Assert.Null(pilates.Porcentaje);
    }

    [Fact]
    public void Sin_reservas_no_hay_mas_reservada_ni_ocupacion()
    {
        var informe = _repo.Informe(Desde, Hasta);

        Assert.Null(informe.MasReservada);
        Assert.Null(informe.OcupacionMedia);
        Assert.Equal(0, informe.TotalReservas);
    }

    [Fact]
    public void Calcula_la_ocupacion_media_de_todas_las_actividades()
    {
        Reservas(Crear(Desde.AddHours(10), _yoga), 4, EstadoReserva.Confirmada);      // 4 de 4
        Reservas(Crear(Desde.AddHours(12), _pilates), 0, EstadoReserva.Confirmada);   // 0 de 4

        Assert.Equal(50, _repo.Informe(Desde, Hasta).OcupacionMedia);
    }

    [Fact]
    public void Con_hasta_anterior_a_desde_lanza_ValidacionException()
    {
        var ex = Assert.Throws<ValidacionException>(() => _repo.Informe(Hasta, Desde));

        Assert.Equal("La fecha «hasta» no puede ser anterior a «desde».", ex.Message);
    }
}
