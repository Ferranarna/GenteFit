using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

public class CrearSesionTests : IDisposable
{
    private static readonly DateTime Dia = new(2026, 11, 20);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SesionRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Sala _grande;
    private readonly Sala _pequena;
    private readonly Entrenador _laura;
    private readonly Entrenador _marc;

    public CrearSesionTests()
    {
        var db = _bd.Contexto;
        _repo = new SesionRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga");
        _grande = DatosAgenda.CrearSala(db, "Sala Grande");
        _pequena = DatosAgenda.CrearSala(db, "Sala Pequeña");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");   // especialidad Yoga por defecto
        _marc = DatosAgenda.CrearEntrenador(db, "marc");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion Nueva(Sala sala, Entrenador entrenador, int hora, int minutos = 0, int duracion = 60) => new()
    {
        ActividadId = _yoga.ActividadId,
        SalaId = sala.SalaId,
        EntrenadorId = entrenador.EntrenadorId,
        Fecha = Dia.AddHours(hora).AddMinutes(minutos),
        Duracion = duracion
    };

    private int SesionesGuardadas()
    {
        using var db = _bd.NuevoContexto();
        return db.Sesiones.Count();
    }

    [Fact]
    public void Crea_una_sesion_valida_como_programada()
    {
        var creada = _repo.Crear(Nueva(_grande, _laura, 10));

        using var db = _bd.NuevoContexto();
        var leida = db.Sesiones.Single();
        Assert.Equal(creada.SesionId, leida.SesionId);
        Assert.Equal(EstadoSesion.Programada, leida.Estado);
    }

    [Fact]
    public void No_deja_dos_sesiones_en_la_misma_sala_a_la_vez()
    {
        _repo.Crear(Nueva(_grande, _laura, 10));                    // 10:00–11:00

        var ex = Assert.Throws<ValidacionException>(() =>
            _repo.Crear(Nueva(_grande, _marc, 10, minutos: 30)));   // 10:30–11:30, otro entrenador

        Assert.Equal("La sala ya está ocupada a esa hora.", ex.Message);
        Assert.Equal(1, SesionesGuardadas());
    }

    [Fact]
    public void No_deja_al_mismo_entrenador_en_dos_sesiones_a_la_vez()
    {
        _repo.Crear(Nueva(_grande, _laura, 10));                    // 10:00–11:00

        var ex = Assert.Throws<ValidacionException>(() =>
            _repo.Crear(Nueva(_pequena, _laura, 10, minutos: 30))); // otra sala, misma entrenadora

        Assert.Equal("El entrenador ya tiene otra sesión a esa hora.", ex.Message);
        Assert.Equal(1, SesionesGuardadas());
    }

    [Fact]
    public void Deja_una_sesion_que_empieza_justo_cuando_acaba_otra()
    {
        _repo.Crear(Nueva(_grande, _laura, 10));                    // 10:00–11:00
        _repo.Crear(Nueva(_grande, _laura, 11));                    // 11:00–12:00: borde, no choca

        Assert.Equal(2, SesionesGuardadas());
    }

    [Fact]
    public void Una_sesion_cancelada_no_ocupa_la_sala()
    {
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _grande, _laura, Dia.AddHours(10), EstadoSesion.Cancelada);

        _repo.Crear(Nueva(_grande, _laura, 10));

        Assert.Equal(2, SesionesGuardadas());
    }

    [Fact]
    public void Al_modificar_no_choca_consigo_misma()
    {
        var sesion = _repo.Crear(Nueva(_grande, _laura, 10));

        sesion.Duracion = 90;
        _repo.Actualizar(sesion);

        using var db = _bd.NuevoContexto();
        Assert.Equal(90, db.Sesiones.Single().Duracion);
    }

    [Fact]
    public void No_deja_un_entrenador_de_otra_especialidad()
    {
        var pau = DatosAgenda.CrearEntrenador(_bd.Contexto, "pau", Especialidad.Spinning);

        var ex = Assert.Throws<ValidacionException>(() => _repo.Crear(Nueva(_grande, pau, 10)));

        Assert.Equal("La especialidad del entrenador no coincide con la de la actividad.", ex.Message);
    }
    private static readonly DateTime Ahora = Dia.AddDays(-1).AddHours(12);   // 19-nov 12:00

    [Fact]
    public void No_deja_programar_una_sesion_en_el_pasado()
    {
        var ex = Assert.Throws<ValidacionException>(() =>
            _repo.Programar(Nueva(_grande, _laura, 12), Dia.AddHours(12)));   // empieza justo "ahora"

        Assert.Equal("No se puede programar una sesión en el pasado.", ex.Message);
        Assert.Equal(0, SesionesGuardadas());
    }

    [Fact]
    public void Modificar_cambia_los_datos_de_una_sesion_futura()
    {
        var sesion = _repo.Programar(Nueva(_grande, _laura, 10), Ahora);

        _repo.Modificar(new Sesion
        {
            SesionId = sesion.SesionId,
            ActividadId = _yoga.ActividadId,
            SalaId = _pequena.SalaId,
            EntrenadorId = _marc.EntrenadorId,
            Fecha = Dia.AddHours(17),
            Duracion = 45
        }, Ahora);

        using var db = _bd.NuevoContexto();
        var leida = db.Sesiones.Single();
        Assert.Equal(_pequena.SalaId, leida.SalaId);
        Assert.Equal(_marc.EntrenadorId, leida.EntrenadorId);
        Assert.Equal(Dia.AddHours(17), leida.Fecha);
        Assert.Equal(45, leida.Duracion);
        Assert.Equal(EstadoSesion.Programada, leida.Estado);
    }

    [Fact]
    public void No_deja_modificar_una_sesion_que_ya_ha_empezado()
    {
        var sesion = _repo.Crear(Nueva(_grande, _laura, 10));
        var cambios = Nueva(_grande, _laura, 15);
        cambios.SesionId = sesion.SesionId;

        var ex = Assert.Throws<ValidacionException>(() =>
            _repo.Modificar(cambios, Dia.AddHours(10)));   // "ahora" = la hora de inicio: borde

        Assert.Equal("No se puede modificar una sesión que ya ha empezado.", ex.Message);
    }

    [Fact]
    public void No_deja_modificar_una_sesion_cancelada()
    {
        var cancelada = DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _grande, _laura, Dia.AddHours(10), EstadoSesion.Cancelada);
        var cambios = Nueva(_grande, _laura, 15);
        cambios.SesionId = cancelada.SesionId;

        var ex = Assert.Throws<ValidacionException>(() => _repo.Modificar(cambios, Ahora));

        Assert.Equal("No se puede modificar una sesión cancelada.", ex.Message);
    }

    [Fact]
    public void No_deja_mover_una_sesion_a_una_hora_que_ya_ha_pasado()
    {
        var sesion = _repo.Programar(Nueva(_grande, _laura, 10), Ahora);
        var cambios = Nueva(_grande, _laura, 10);
        cambios.SesionId = sesion.SesionId;
        cambios.Fecha = Ahora.AddHours(-1);

        var ex = Assert.Throws<ValidacionException>(() => _repo.Modificar(cambios, Ahora));

        Assert.Equal("La nueva fecha y hora ya han pasado.", ex.Message);
    }
}