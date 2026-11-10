using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class EstadisticasSesionesTests : IDisposable
{
    // Miércoles 11 de noviembre de 2026 a las 12:00. La semana va del lunes 9 al domingo 15.
    private static readonly DateTime Hoy = new(2026, 11, 11, 12, 0, 0);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SesionRepositorio _repo;
    private Entrenador _marc = null!;

    public EstadisticasSesionesTests()
    {
        _repo = new SesionRepositorio(_bd.Contexto);
        PrepararSemana();
    }

    public void Dispose() => _bd.Dispose();

    // Monta la tabla de datos que explica el resultado esperado de cada test
    private void PrepararSemana()
    {
        var db = _bd.Contexto;
        var clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 10, Rol.Cliente, "cli");
        var yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 10);
        var grande = DatosAgenda.CrearSala(db, "Sala Grande");
        var ciclo = DatosAgenda.CrearSala(db, "Sala Ciclo");
        var laura = DatosAgenda.CrearEntrenador(db, "laura");
        _marc = DatosAgenda.CrearEntrenador(db, "marc");

        Sesion Crear(Entrenador e, Sala s, int dia, int hora, int mes = 11, EstadoSesion estado = EstadoSesion.Programada) =>
            DatosAgenda.CrearSesion(db, yoga, s, e, new DateTime(2026, mes, dia, hora, 0, 0), estado);

        // Fuera de la semana (y del mes, las dos primeras)
        Crear(laura, grande, 31, 10, mes: 10);
        Crear(laura, grande, 1, 10, mes: 12);
        var domingoAnterior = Crear(laura, grande, 8, 20);
        var lunesSiguiente = Crear(laura, grande, 16, 10);

        // La semana del 9 al 15
        var lunes = Crear(laura, grande, 9, 10);
        var martesCancelada = Crear(laura, grande, 10, 10, estado: EstadoSesion.Cancelada);
        var miercoles10 = Crear(laura, grande, 11, 10);
        var miercoles18 = Crear(_marc, ciclo, 11, 18);
        Crear(laura, grande, 11, 20);
        Crear(laura, grande, 15, 20);

        // Reservas
        DatosAgenda.CrearReservas(db, domingoAnterior, clientes.Take(2), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(db, lunesSiguiente, clientes.Take(2), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(db, lunes, clientes.Take(4), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(db, lunes, clientes.Skip(4).Take(1), EstadoReserva.Cancelada);
        DatosAgenda.CrearReservas(db, martesCancelada, clientes.Take(3), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(db, miercoles10, clientes.Take(2), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(db, miercoles18, clientes.Take(6), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(db, miercoles18, clientes.Skip(6).Take(1), EstadoReserva.ListaEspera);
    }

    [Fact]
    public void OcupacionDeLaSemana_cuenta_solo_sesiones_programadas_de_lunes_a_domingo()
    {
        var (reservadas, plazas) = _repo.OcupacionDeLaSemana(Hoy);

        Assert.Equal(12, reservadas);   // lunes 4 + miércoles 10:00 2 + miércoles 18:00 6
        Assert.Equal(50, plazas);       // 5 sesiones programadas x 10 plazas
    }

    [Fact]
    public void OcupacionDeLaSemana_con_la_base_vacia_da_cero_y_cero()
    {
        using var vacia = new BaseDeDatosPrueba();

        var (reservadas, plazas) = new SesionRepositorio(vacia.Contexto).OcupacionDeLaSemana(Hoy);

        Assert.Equal(0, reservadas);
        Assert.Equal(0, plazas);
    }

    [Fact]
    public void OcupacionDeLaSemana_de_un_entrenador_cuenta_solo_las_suyas()
    {
        var (reservadas, plazas) = _repo.OcupacionDeLaSemana(Hoy, _marc.EntrenadorId);

        Assert.Equal(6, reservadas);
        Assert.Equal(10, plazas);
    }

    [Fact]
    public void ContarDeLaSemana_cuenta_las_sesiones_y_las_salas_distintas()
    {
        var (sesiones, salas) = _repo.ContarDeLaSemana(Hoy);

        Assert.Equal(5, sesiones);   // la cancelada del martes no cuenta
        Assert.Equal(2, salas);      // Grande y Ciclo
    }

    [Fact]
    public void ContarDeLaSemana_de_un_entrenador_cuenta_solo_las_suyas()
    {
        var (sesiones, salas) = _repo.ContarDeLaSemana(Hoy, _marc.EntrenadorId);

        Assert.Equal(1, sesiones);
        Assert.Equal(1, salas);
    }

    [Fact]
    public void ContarDelMes_separa_programadas_y_canceladas_y_deja_fuera_otros_meses()
    {
        var (programadas, canceladas) = _repo.ContarDelMes(Hoy);

        // Noviembre: 8, 9, 11 (x3), 15 y 16 programadas = 7; el 10 cancelada = 1
        Assert.Equal(7, programadas);
        Assert.Equal(1, canceladas);
    }

    [Fact]
    public void ContarDeHoy_cuenta_las_de_hoy_y_da_la_hora_de_la_proxima()
    {
        var (sesiones, proxima) = _repo.ContarDeHoy(Hoy);

        Assert.Equal(3, sesiones);   // 10:00, 18:00 y 20:00
        Assert.Equal(new DateTime(2026, 11, 11, 18, 0, 0), proxima!.Value);   // son las 12:00: la 10:00 ya pasó
    }

    [Fact]
    public void PlazasLibresHoy_solo_cuenta_las_sesiones_que_aun_no_han_empezado()
    {
        var (libres, quedan) = _repo.PlazasLibresHoy(Hoy);

        Assert.Equal(2, quedan);     // 18:00 y 20:00
        Assert.Equal(14, libres);    // 20 plazas - 6 reservadas en la de las 18:00
    }
}