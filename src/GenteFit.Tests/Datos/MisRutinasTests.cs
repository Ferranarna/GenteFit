using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class MisRutinasTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly RutinaRepositorio _repo;
    private readonly Usuario _ana;
    private readonly Usuario _luis;
    private readonly Ejercicio _sentadilla;
    private readonly Ejercicio _zancada;
    private readonly Ejercicio _plancha;

    public MisRutinasTests()
    {
        var db = _bd.Contexto;
        _repo = new RutinaRepositorio(db);
        var clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 2, Rol.Cliente, "cli");
        _ana = clientes[0];
        _luis = clientes[1];

        var ejercicios = new EjercicioRepositorio(db);
        _sentadilla = ejercicios.Crear(new Ejercicio { NombreEjercicio = "Sentadilla", GrupoMuscular = "Piernas" });
        _zancada = ejercicios.Crear(new Ejercicio { NombreEjercicio = "Zancada", GrupoMuscular = "Piernas" });
        _plancha = ejercicios.Crear(new Ejercicio { NombreEjercicio = "Plancha", GrupoMuscular = "Core" });
    }

    public void Dispose() => _bd.Dispose();

    private DetalleRutina Anadir(Rutina rutina, Ejercicio ejercicio) =>
        _repo.AnadirEjercicio(rutina.RutinaId, ejercicio.EjercicioId, _repo.SiguienteOrden(rutina.RutinaId), 3, 10);

    [Fact]
    public void SiguienteOrden_empieza_en_1_y_va_al_final()
    {
        var rutina = _repo.Crear(_ana.UsuarioId, "Pierna");

        Assert.Equal(1, _repo.SiguienteOrden(rutina.RutinaId));
        Anadir(rutina, _sentadilla);
        Anadir(rutina, _zancada);
        Assert.Equal(3, _repo.SiguienteOrden(rutina.RutinaId));
    }

    [Fact]
    public void Quitar_un_ejercicio_vuelve_a_numerar_los_demas_sin_huecos()
    {
        var rutina = _repo.Crear(_ana.UsuarioId, "Pierna");
        Anadir(rutina, _sentadilla);
        var zancada = Anadir(rutina, _zancada);
        Anadir(rutina, _plancha);

        _repo.QuitarEjercicio(zancada.DetalleRutinaId);

        using var otro = _bd.NuevoContexto();
        var quedan = otro.DetallesRutina.Where(d => d.RutinaId == rutina.RutinaId)
            .OrderBy(d => d.Orden).Select(d => new { d.Orden, d.EjercicioId }).ToList();
        Assert.Equal(new[] { 1, 2 }, quedan.Select(d => d.Orden));
        Assert.Equal(new[] { _sentadilla.EjercicioId, _plancha.EjercicioId }, quedan.Select(d => d.EjercicioId));
    }

    [Fact]
    public void ObtenerDeCliente_no_devuelve_la_rutina_de_otro_cliente()
    {
        var deAna = _repo.Crear(_ana.UsuarioId, "Pierna");
        Anadir(deAna, _sentadilla);

        Assert.NotNull(_repo.ObtenerDeCliente(deAna.RutinaId, _ana.UsuarioId));
        Assert.Null(_repo.ObtenerDeCliente(deAna.RutinaId, _luis.UsuarioId));
        Assert.Single(_repo.ObtenerDeCliente(deAna.RutinaId, _ana.UsuarioId)!.Detalles);
    }

    [Fact]
    public void EsDeCliente_y_DetalleEsDeCliente_solo_dan_true_al_propietario()
    {
        var deAna = _repo.Crear(_ana.UsuarioId, "Pierna");
        var detalle = Anadir(deAna, _sentadilla);

        Assert.True(_repo.EsDeCliente(deAna.RutinaId, _ana.UsuarioId));
        Assert.False(_repo.EsDeCliente(deAna.RutinaId, _luis.UsuarioId));
        Assert.True(_repo.DetalleEsDeCliente(detalle.DetalleRutinaId, _ana.UsuarioId));
        Assert.False(_repo.DetalleEsDeCliente(detalle.DetalleRutinaId, _luis.UsuarioId));
        Assert.False(_repo.EsDeCliente(Guid.NewGuid(), _ana.UsuarioId));
    }

    [Fact]
    public void Configurar_con_series_0_lanza_ValidacionException_y_no_cambia_nada()
    {
        var rutina = _repo.Crear(_ana.UsuarioId, "Pierna");
        var detalle = Anadir(rutina, _sentadilla);

        var ex = Assert.Throws<ValidacionException>(() => _repo.ConfigurarEjercicio(detalle.DetalleRutinaId, 0, 10));

        Assert.Equal("El campo series debe ser mayor que 0.", ex.Message);
        using var otro = _bd.NuevoContexto();
        Assert.Equal(3, otro.DetallesRutina.Single().Series);
    }

    [Fact]
    public void Eliminar_la_rutina_borra_tambien_sus_ejercicios_pero_no_el_catalogo()
    {
        var rutina = _repo.Crear(_ana.UsuarioId, "Pierna");
        Anadir(rutina, _sentadilla);
        Anadir(rutina, _zancada);

        _repo.Eliminar(rutina.RutinaId);

        using var otro = _bd.NuevoContexto();
        Assert.Empty(otro.Rutinas);
        Assert.Empty(otro.DetallesRutina);
        Assert.Equal(3, otro.Ejercicios.Count());
    }

    [Fact]
    public void Crear_sin_nombre_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(() => _repo.Crear(_ana.UsuarioId, "  "));
    }

    [Fact]
    public void Configurar_con_demasiadas_series_lanza_ValidacionException()
    {
        var rutina = _repo.Crear(_ana.UsuarioId, "Pierna");
        var detalle = Anadir(rutina, _sentadilla);

        var ex = Assert.Throws<ValidacionException>(
            () => _repo.ConfigurarEjercicio(detalle.DetalleRutinaId, RutinaRepositorio.MaxSeries + 1, 10));

        Assert.Equal($"Las series no pueden ser más de {RutinaRepositorio.MaxSeries}.", ex.Message);
    }
}
