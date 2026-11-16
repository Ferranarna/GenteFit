using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class SalasTests : IDisposable
{
    private static readonly DateTime Desde = new(2026, 11, 1);
    private static readonly DateTime Hasta = new(2026, 11, 30);   // 30 días x 15 h = 27.000 minutos disponibles

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SalaRepositorio _repo;
    private readonly Sala _grande;
    private readonly Sala _ciclo;
    private readonly Actividad _yoga;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public SalasTests()
    {
        var db = _bd.Contexto;
        _repo = new SalaRepositorio(db);
        _grande = DatosAgenda.CrearSala(db, "Sala Grande");
        _ciclo = DatosAgenda.CrearSala(db, "Sala Ciclo");
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 4);
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 4, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion Crear(Sala sala, DateTime fecha, EstadoSesion estado = EstadoSesion.Programada) =>
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, sala, _laura, fecha, estado);   // 60 minutos

    // ---------- Crear y modificar ----------

    [Fact]
    public void Crear_con_un_nombre_que_ya_existe_lanza_ValidacionException()
    {
        var ex = Assert.Throws<ValidacionException>(() =>
            _repo.Crear(new Sala { NombreSala = "Sala Grande", Ubicacion = Ubicacion.Planta2 }));

        Assert.Equal("Ya existe una sala con ese nombre.", ex.Message);
    }

    [Fact]
    public void Modificar_conservando_su_propio_nombre_no_da_error()
    {
        _grande.Ubicacion = Ubicacion.Planta2;
        _repo.Actualizar(_grande);

        using var otro = _bd.NuevoContexto();
        Assert.Equal(Ubicacion.Planta2, otro.Salas.Single(s => s.SalaId == _grande.SalaId).Ubicacion);
    }

    [Fact]
    public void Modificar_con_el_nombre_de_otra_sala_lanza_ValidacionException()
    {
        _ciclo.NombreSala = "Sala Grande";

        Assert.Throws<ValidacionException>(() => _repo.Actualizar(_ciclo));
    }

    [Fact]
    public void Crear_sin_nombre_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(() =>
            _repo.Crear(new Sala { NombreSala = " ", Ubicacion = Ubicacion.Planta1 }));
    }

    // ---------- Informe de uso ----------

    [Fact]
    public void Informe_ordena_por_horas_de_uso_y_calcula_la_ocupacion()
    {
        Crear(_grande, Desde.AddHours(10));
        Crear(_ciclo, Desde.AddHours(10));
        Crear(_ciclo, Desde.AddHours(12));

        var informe = _repo.Informe(Desde, Hasta);

        Assert.Equal(new[] { "Sala Ciclo", "Sala Grande" }, informe.Filas.Select(f => f.Sala.NombreSala));
        var ciclo = informe.Filas[0];
        Assert.Equal(2, ciclo.Sesiones);
        Assert.Equal(120, ciclo.Minutos);
        Assert.Equal(30, informe.Dias);
        Assert.Equal(0, ciclo.Porcentaje);   // 120 de 27.000 minutos: menos del 1 %
        Assert.Equal("Sala Ciclo", informe.MasUsada!.Sala.NombreSala);
        Assert.Equal(3, informe.TotalSesiones);
    }

    [Fact]
    public void Informe_no_cuenta_sesiones_canceladas_ni_fuera_del_periodo()
    {
        Crear(_grande, Desde.AddHours(10), EstadoSesion.Cancelada);
        Crear(_grande, new DateTime(2026, 10, 31, 22, 0, 0));
        Crear(_grande, new DateTime(2026, 12, 1, 9, 0, 0));
        Crear(_grande, new DateTime(2026, 11, 30, 23, 0, 0));   // último día: dentro

        var grande = _repo.Informe(Desde, Hasta).Filas.Single(f => f.Sala.SalaId == _grande.SalaId);

        Assert.Equal(1, grande.Sesiones);
    }

    [Fact]
    public void Informe_calcula_los_asistentes_por_sesion_con_las_confirmadas()
    {
        var a = Crear(_grande, Desde.AddHours(10));
        var b = Crear(_grande, Desde.AddHours(12));
        DatosAgenda.CrearReservas(_bd.Contexto, a, _clientes.Take(3), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(_bd.Contexto, b, _clientes.Take(1), EstadoReserva.Confirmada);
        DatosAgenda.CrearReservas(_bd.Contexto, b, _clientes.Skip(1).Take(2), EstadoReserva.ListaEspera);

        var grande = _repo.Informe(Desde, Hasta).Filas.Single(f => f.Sala.SalaId == _grande.SalaId);

        Assert.Equal(4, grande.Asistentes);
        Assert.Equal(2.0, grande.MediaAsistentes);
    }

    [Fact]
    public void Informe_cuenta_las_salas_sin_uso()
    {
        Crear(_grande, Desde.AddHours(10));

        var informe = _repo.Informe(Desde, Hasta);

        Assert.Equal(1, informe.SinUso);
        Assert.Null(informe.Filas.Single(f => f.Sala.SalaId == _ciclo.SalaId).MediaAsistentes);
    }

    [Fact]
    public void Informe_de_un_dia_lleno_da_el_100_por_cien()
    {
        for (var hora = 9; hora < 24; hora++)
            Crear(_grande, new DateTime(2026, 11, 5, hora, 0, 0));

        var grande = _repo.Informe(new DateTime(2026, 11, 5), new DateTime(2026, 11, 5))
            .Filas.Single(f => f.Sala.SalaId == _grande.SalaId);

        Assert.Equal(100, grande.Porcentaje);
    }

    [Fact]
    public void Informe_con_hasta_anterior_a_desde_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(() => _repo.Informe(Hasta, Desde));
    }
}
