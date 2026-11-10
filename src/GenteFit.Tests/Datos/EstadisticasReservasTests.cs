using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class EstadisticasReservasTests : IDisposable
{
    // Miércoles 11 de noviembre de 2026 a las 12:00. La semana va del lunes 9 al domingo 15.
    private static readonly DateTime Hoy = new(2026, 11, 11, 12, 0, 0);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly ReservaRepositorio _reservas;
    private readonly ActividadRepositorio _actividades;
    private readonly SalaRepositorio _salas;
    private Entrenador _laura = null!;
    private Entrenador _marc = null!;

    public EstadisticasReservasTests()
    {
        _reservas = new ReservaRepositorio(_bd.Contexto);
        _actividades = new ActividadRepositorio(_bd.Contexto);
        _salas = new SalaRepositorio(_bd.Contexto);
        PrepararMes();
    }

    public void Dispose() => _bd.Dispose();

    // Monta la tabla de datos que explica el resultado esperado de cada test
    private void PrepararMes()
    {
        var db = _bd.Contexto;
        var clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 10, Rol.Cliente, "cli");
        var yoga = DatosAgenda.CrearActividad(db, "Yoga");
        var spinning = DatosAgenda.CrearActividad(db, "Spinning");
        var zumba = DatosAgenda.CrearActividad(db, "Zumba");
        var grande = DatosAgenda.CrearSala(db, "Sala Grande");
        var ciclo = DatosAgenda.CrearSala(db, "Sala Ciclo");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _marc = DatosAgenda.CrearEntrenador(db, "marc");

        Sesion Crear(Actividad a, Sala s, Entrenador e, int mes, int dia, int hora, EstadoSesion estado = EstadoSesion.Programada) =>
            DatosAgenda.CrearSesion(db, a, s, e, new DateTime(2026, mes, dia, hora, 0, 0), estado);

        var s1 = Crear(yoga, grande, _laura, 11, 9, 10);
        var s2 = Crear(spinning, ciclo, _marc, 11, 10, 18);
        var s3 = Crear(spinning, ciclo, _marc, 11, 11, 10);
        var s4 = Crear(spinning, ciclo, _marc, 11, 11, 18);
        var s5 = Crear(yoga, grande, _laura, 11, 12, 10);
        var otroMes = Crear(zumba, grande, _laura, 10, 20, 10);
        var cancelada1 = Crear(spinning, grande, _marc, 11, 13, 10, EstadoSesion.Cancelada);
        Crear(spinning, grande, _marc, 11, 14, 10, EstadoSesion.Cancelada);

        var confirmada = EstadoReserva.Confirmada;
        DatosAgenda.CrearReservas(db, s1, clientes.Take(4), confirmada);
        DatosAgenda.CrearReservas(db, s2, clientes.Take(5), confirmada);
        DatosAgenda.CrearReservas(db, s2, clientes.Skip(5).Take(1), EstadoReserva.ListaEspera);
        DatosAgenda.CrearReservas(db, s3, clientes.Take(3), confirmada);
        DatosAgenda.CrearReservas(db, s4, clientes.Take(2), confirmada);
        DatosAgenda.CrearReservas(db, s4, clientes.Skip(2).Take(1), EstadoReserva.ListaEspera);
        DatosAgenda.CrearReservas(db, s5, clientes.Take(5), confirmada);
        DatosAgenda.CrearReservas(db, otroMes, clientes.Take(9), confirmada);
        DatosAgenda.CrearReservas(db, cancelada1, clientes.Take(7), EstadoReserva.Cancelada);
    }

    [Fact]
    public void ContarDelMes_cuenta_confirmadas_y_en_espera_solo_de_ese_mes()
    {
        var (confirmadas, enEspera) = _reservas.ContarDelMes(Hoy);

        Assert.Equal(19, confirmadas);   // 4 + 5 + 3 + 2 + 5; la de octubre no cuenta
        Assert.Equal(2, enEspera);
    }

    [Fact]
    public void ContarDeHoy_cuenta_las_reservas_de_las_sesiones_de_hoy()
    {
        var (confirmadas, enEspera) = _reservas.ContarDeHoy(Hoy);

        Assert.Equal(5, confirmadas);    // 3 (10:00) + 2 (18:00)
        Assert.Equal(1, enEspera);
    }

    [Fact]
    public void ContarAlumnosDeLaSemana_da_alumnos_distintos_y_reservas_de_un_entrenador()
    {
        var (alumnos, reservas) = _reservas.ContarAlumnosDeLaSemana(_marc.EntrenadorId, Hoy);

        Assert.Equal(5, alumnos);     // los mismos 5 clientes repiten en varias sesiones
        Assert.Equal(10, reservas);   // 5 + 3 + 2
    }

    [Fact]
    public void ContarAlumnosDeLaSemana_no_mezcla_las_sesiones_de_otro_entrenador()
    {
        var (alumnos, reservas) = _reservas.ContarAlumnosDeLaSemana(_laura.EntrenadorId, Hoy);

        Assert.Equal(5, alumnos);
        Assert.Equal(9, reservas);    // 4 + 5; la de octubre queda fuera de la semana
    }

    [Fact]
    public void MasReservadaDelMes_devuelve_la_actividad_con_mas_reservas_confirmadas()
    {
        var mejor = _actividades.MasReservadaDelMes(Hoy);

        Assert.NotNull(mejor);
        Assert.Equal("Spinning", mejor!.Value.Nombre);
        Assert.Equal(10, mejor.Value.Reservas);   // las 2 en espera y las canceladas no cuentan
    }

    [Fact]
    public void MasReservadaDelMes_sin_reservas_devuelve_null()
    {
        using var vacia = new BaseDeDatosPrueba();

        var mejor = new ActividadRepositorio(vacia.Contexto).MasReservadaDelMes(Hoy);

        Assert.Null(mejor);
    }

    [Fact]
    public void MasReservadaDelMes_en_caso_de_empate_gana_la_primera_por_orden_alfabetico()
    {
        using var otra = new BaseDeDatosPrueba();
        var db = otra.Contexto;
        var clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 1, Rol.Cliente, "cli");
        var yoga = DatosAgenda.CrearActividad(db, "Yoga");
        var spinning = DatosAgenda.CrearActividad(db, "Spinning");
        var sala = DatosAgenda.CrearSala(db, "Sala Grande");
        var laura = DatosAgenda.CrearEntrenador(db, "laura");
        var sesionYoga = DatosAgenda.CrearSesion(db, yoga, sala, laura, new DateTime(2026, 11, 9, 10, 0, 0));
        var sesionSpinning = DatosAgenda.CrearSesion(db, spinning, sala, laura, new DateTime(2026, 11, 10, 10, 0, 0));
        DatosAgenda.CrearReservas(db, sesionYoga, clientes, EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(db, sesionSpinning, clientes, EstadoReserva.Confirmada);

        var mejor = new ActividadRepositorio(db).MasReservadaDelMes(Hoy);

        Assert.Equal("Spinning", mejor!.Value.Nombre);   // 1 a 1: gana la S antes que la Y
    }

    [Fact]
    public void MasUsadaDelMes_no_cuenta_sesiones_canceladas_ni_de_otros_meses()
    {
        var mejor = _salas.MasUsadaDelMes(Hoy);

        Assert.NotNull(mejor);
        Assert.Equal("Sala Ciclo", mejor!.Value.Nombre);
        Assert.Equal(3, mejor.Value.Sesiones);
    }

    [Fact]
    public void MasUsadaDelMes_sin_sesiones_devuelve_null()
    {
        using var vacia = new BaseDeDatosPrueba();

        var mejor = new SalaRepositorio(vacia.Contexto).MasUsadaDelMes(Hoy);

        Assert.Null(mejor);
    }
}