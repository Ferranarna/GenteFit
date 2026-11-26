using GenteFit.Datos.Repositorios;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

// Aplicar en GenteFit las sesiones del lote de vuelta de Odoo, con las reglas del P3.
public class AplicarDesdeOdooTests : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 11, 26, 12, 0, 0);
    private static readonly DateTime Lunes = new(2026, 11, 30);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly SesionRepositorio _repo;
    private readonly Actividad _yoga;
    private readonly Sala _salaA;
    private readonly Sala _salaB;
    private readonly Entrenador _laura;
    private readonly Entrenador _marc;
    private readonly List<Usuario> _clientes;

    public AplicarDesdeOdooTests()
    {
        var db = _bd.Contexto;
        _repo = new SesionRepositorio(db);
        _yoga = DatosAgenda.CrearActividad(db, "Yoga");
        _salaA = DatosAgenda.CrearSala(db, "Sala A");
        _salaB = DatosAgenda.CrearSala(db, "Sala B");
        _laura = DatosAgenda.CrearEntrenador(db, "laura");
        _marc = DatosAgenda.CrearEntrenador(db, "marc");
        _clientes = DatosPrueba.CrearUsuarios(new UsuarioRepositorio(db), 2, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private Sesion Existente(DateTime fecha, Sala sala, Entrenador entrenador) =>
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, sala, entrenador, fecha);

    // Lo que llega de Odoo: una entidad suelta, sin seguimiento de EF
    private Sesion Recibida(Guid id, DateTime fecha, Sala sala, Entrenador entrenador,
        EstadoSesion estado = EstadoSesion.Programada, Guid? actividadId = null) => new()
        {
            SesionId = id,
            Fecha = fecha,
            Duracion = 60,
            Estado = estado,
            ActividadId = actividadId ?? _yoga.ActividadId,
            SalaId = sala.SalaId,
            EntrenadorId = entrenador.EntrenadorId
        };

    private Sesion Leer(Guid id) => _bd.NuevoContexto().Sesiones.Single(s => s.SesionId == id);

    [Fact]
    public void Crea_la_sesion_nueva_con_el_GUID_que_genero_Odoo()
    {
        var guidDeOdoo = Guid.NewGuid();

        var r = _repo.AplicarDesdeOdoo(Recibida(guidDeOdoo, Lunes.AddHours(18), _salaA, _laura), Ahora);

        Assert.Equal(AccionImportacion.Creada, r.Accion);
        Assert.Equal(Lunes.AddHours(18), Leer(guidDeOdoo).Fecha);
    }

    [Fact]
    public void Modifica_una_sesion_existente()
    {
        var sesion = Existente(Lunes.AddHours(19), _salaA, _laura);

        var r = _repo.AplicarDesdeOdoo(Recibida(sesion.SesionId, Lunes.AddHours(20), _salaA, _laura), Ahora);

        Assert.Equal(AccionImportacion.Modificada, r.Accion);
        Assert.Equal(Lunes.AddHours(20), Leer(sesion.SesionId).Fecha);
    }

    [Fact]
    public void Sin_cambios_si_llega_igual_que_esta()
    {
        var sesion = Existente(Lunes.AddHours(19), _salaA, _laura);

        var r = _repo.AplicarDesdeOdoo(Recibida(sesion.SesionId, Lunes.AddHours(19), _salaA, _laura), Ahora);

        Assert.Equal(AccionImportacion.SinCambios, r.Accion);
    }

    [Fact]
    public void Cancela_la_sesion_y_anula_sus_reservas()
    {
        var sesion = Existente(Lunes.AddHours(19), _salaA, _laura);
        DatosAgenda.CrearReservas(_bd.Contexto, sesion, _clientes, EstadoReserva.Confirmada);

        var r = _repo.AplicarDesdeOdoo(
            Recibida(sesion.SesionId, Lunes.AddHours(19), _salaA, _laura, EstadoSesion.Cancelada), Ahora);

        Assert.Equal(AccionImportacion.Cancelada, r.Accion);
        Assert.Equal("2 reservas anuladas.", r.Detalle);
        Assert.Equal(EstadoSesion.Cancelada, Leer(sesion.SesionId).Estado);
    }

    [Fact]
    public void Rechaza_una_actividad_que_no_existe_en_GenteFit()
    {
        var r = _repo.AplicarDesdeOdoo(
            Recibida(Guid.NewGuid(), Lunes.AddHours(18), _salaA, _laura, actividadId: Guid.NewGuid()), Ahora);

        Assert.Equal(AccionImportacion.Rechazada, r.Accion);
        Assert.Equal("La actividad no existe.", r.Detalle);
    }

    [Fact]
    public void Rechaza_reactivar_una_sesion_cancelada()
    {
        var sesion = Existente(Lunes.AddHours(19), _salaA, _laura);
        _repo.Cancelar(sesion.SesionId, Ahora);

        var r = _repo.AplicarDesdeOdoo(Recibida(sesion.SesionId, Lunes.AddHours(19), _salaA, _laura), Ahora);

        Assert.Equal(AccionImportacion.Rechazada, r.Accion);
    }

    [Fact]
    public void En_un_lote_la_sesion_que_choca_con_otra_del_mismo_lote_se_rechaza()
    {
        var sesion = Existente(Lunes.AddHours(19), _salaA, _laura);

        var resultados = _repo.AplicarLoteDesdeOdoo(
        [
            Recibida(sesion.SesionId, Lunes.AddHours(20), _salaA, _laura),  // se mueve a las 20:00
            Recibida(Guid.NewGuid(), Lunes.AddHours(20), _salaA, _marc)       // nueva, misma sala y hora
        ], Ahora);

        Assert.Equal([AccionImportacion.Modificada, AccionImportacion.Rechazada], resultados.Select(r => r.Accion));
        Assert.Equal("La sala ya está ocupada a esa hora.", resultados[1].Detalle);
    }

    [Fact]
    public void Un_rechazo_no_deja_cambios_a_medias_ni_detiene_el_resto_del_lote()
    {
        var diez = Existente(Lunes.AddHours(10), _salaA, _laura);
        var doce = Existente(Lunes.AddHours(12), _salaA, _marc);
        var nuevaId = Guid.NewGuid();

        var resultados = _repo.AplicarLoteDesdeOdoo(
        [
            Recibida(doce.SesionId, Lunes.AddHours(10), _salaA, _marc),   // choca con la de las 10:00
            Recibida(nuevaId, Lunes.AddHours(18), _salaB, _laura)          // válida
        ], Ahora);

        Assert.Equal([AccionImportacion.Rechazada, AccionImportacion.Creada], resultados.Select(r => r.Accion));
        Assert.Equal(Lunes.AddHours(12), Leer(doce.SesionId).Fecha);   // el cambio rechazado no se guardó
        Assert.Equal(Lunes.AddHours(10), Leer(diez.SesionId).Fecha);
    }
}