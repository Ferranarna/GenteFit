using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Tests.Datos;

public class EjerciciosTests : IDisposable
{
    private readonly BaseDeDatosPrueba _bd = new();
    private readonly EjercicioRepositorio _repo;

    public EjerciciosTests()
    {
        _repo = new EjercicioRepositorio(_bd.Contexto);
    }

    public void Dispose() => _bd.Dispose();

    private Ejercicio Crear(string nombre, string? grupo, string? descripcion = null) =>
        _repo.Crear(new Ejercicio { NombreEjercicio = nombre, GrupoMuscular = grupo, Descripcion = descripcion });

    [Fact]
    public void Crear_guarda_el_ejercicio()
    {
        var creado = Crear("Sentadilla", "Piernas", "Bajar con la espalda recta");

        using var otro = _bd.NuevoContexto();
        var guardado = otro.Ejercicios.Single(e => e.EjercicioId == creado.EjercicioId);
        Assert.Equal("Sentadilla", guardado.NombreEjercicio);
        Assert.Equal("Piernas", guardado.GrupoMuscular);
        Assert.Equal("Bajar con la espalda recta", guardado.Descripcion);
    }

    [Fact]
    public void Crear_sin_nombre_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(() => Crear("  ", "Piernas"));
        Assert.Empty(_bd.NuevoContexto().Ejercicios);
    }

    [Fact]
    public void Actualizar_cambia_los_datos_y_conserva_la_imagen()
    {
        var ejercicio = _repo.Crear(new Ejercicio
        {
            NombreEjercicio = "Sentadila", GrupoMuscular = "Piernas", UrlImagen = "/img/sentadilla.png"
        });

        ejercicio.NombreEjercicio = "Sentadilla";
        ejercicio.GrupoMuscular = "Glúteos";
        _repo.Actualizar(ejercicio);

        using var otro = _bd.NuevoContexto();
        var guardado = otro.Ejercicios.Single();
        Assert.Equal("Sentadilla", guardado.NombreEjercicio);
        Assert.Equal("Glúteos", guardado.GrupoMuscular);
        Assert.Equal("/img/sentadilla.png", guardado.UrlImagen);
    }

    [Fact]
    public void Actualizar_sin_nombre_lanza_ValidacionException()
    {
        var ejercicio = Crear("Plancha", "Core");
        ejercicio.NombreEjercicio = "";

        Assert.Throws<ValidacionException>(() => _repo.Actualizar(ejercicio));
    }

    [Fact]
    public void Buscar_encuentra_por_nombre_o_por_grupo_ordenado_por_nombre()
    {
        Crear("Zancada", "Piernas");
        Crear("Press banca", "Pecho");
        Crear("Sentadilla", "Piernas");

        Assert.Equal(new[] { "Sentadilla", "Zancada" }, _repo.Buscar("Piernas").Select(e => e.NombreEjercicio));
        Assert.Equal(new[] { "Press banca" }, _repo.Buscar("banca").Select(e => e.NombreEjercicio));
    }

    [Fact]
    public void Buscar_sin_coincidencias_devuelve_lista_vacia()
    {
        Crear("Plancha", "Core");

        Assert.Empty(_repo.Buscar("Natación"));
    }

    [Fact]
    public void BuscarPorGrupo_no_devuelve_los_que_no_tienen_grupo()
    {
        Crear("Dominadas", "Espalda");
        Crear("Estiramiento libre", null);

        Assert.Equal(new[] { "Dominadas" }, _repo.BuscarPorGrupo("Espalda").Select(e => e.NombreEjercicio));
    }
}
