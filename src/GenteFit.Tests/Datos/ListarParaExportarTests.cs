using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

// Sesiones que se envían a Odoo: periodo, canceladas y plazas ocupadas.
public class ListarParaExportarTests : IDisposable
{
    private static readonly DateTime Lunes = new(2026, 11, 30);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SesionRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Sala _sala;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public ListarParaExportarTests()
    {
        var db = _bd.Contexto;
        _repo = new SesionRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 3);
        _sala = DatosAgenda.CrearSala(db, "Sala Zen");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 4, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion Crear(DateTime fecha, EstadoSesion estado = EstadoSesion.Programada) =>
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, fecha, estado);

    [Fact]
    public void Incluye_los_dos_dias_extremos_del_periodo_y_nada_de_fuera()
    {
        var primera = Crear(Lunes.AddHours(9));
        var ultima = Crear(Lunes.AddDays(6).AddHours(21));
        Crear(Lunes.AddDays(-1).AddHours(23));   // domingo anterior
        Crear(Lunes.AddDays(7).AddHours(9));     // lunes siguiente

        var filas = _repo.ListarParaExportar(Lunes, Lunes.AddDays(6), incluirCanceladas: true);

        Assert.Equal([primera.SesionId, ultima.SesionId], filas.Select(f => f.Sesion.SesionId));
    }

    [Fact]
    public void Las_canceladas_solo_salen_si_se_piden()
    {
        var programada = Crear(Lunes.AddHours(10));
        Crear(Lunes.AddHours(12), EstadoSesion.Cancelada);

        Assert.Equal(2, _repo.ListarParaExportar(Lunes, Lunes, incluirCanceladas: true).Count);
        var sinCanceladas = Assert.Single(_repo.ListarParaExportar(Lunes, Lunes, incluirCanceladas: false));
        Assert.Equal(programada.SesionId, sinCanceladas.Sesion.SesionId);
    }

    [Fact]
    public void Las_plazas_ocupadas_solo_cuentan_las_reservas_confirmadas()
    {
        var sesion = Crear(Lunes.AddHours(18));
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Take(2), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Skip(2).Take(1), EstadoReserva.ListaEspera);
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes.Skip(3).Take(1), EstadoReserva.Cancelada);

        var fila = Assert.Single(_repo.ListarParaExportar(Lunes, Lunes, incluirCanceladas: true));

        Assert.Equal(2, fila.Ocupadas);
    }

    [Fact]
    public void Trae_cargados_la_actividad_la_sala_y_el_entrenador()
    {
        Crear(Lunes.AddHours(9));

        // Contexto nuevo: así se comprueba que los Include cargan de verdad
        var fila = Assert.Single(new SesionRepositorio(_bd.NuevoContexto())
            .ListarParaExportar(Lunes, Lunes, incluirCanceladas: true));

        Assert.Equal("Yoga", fila.Sesion.Actividad.NombreActividad);
        Assert.Equal("Sala Zen", fila.Sesion.Sala.NombreSala);
        Assert.Equal("Ana", fila.Sesion.Entrenador.Usuario.Nombre);
    }
}