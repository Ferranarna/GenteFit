using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Tests.Datos;

// Gestión de usuarios (Clientes, Entrenadores, Usuarios) y sus dos informes
public class UsuariosTests : IDisposable
{
    private static readonly DateTime Desde = new(2026, 11, 1);
    private static readonly DateTime Hasta = new(2026, 11, 30);
    private static readonly DateTime Futuro = DateTime.Today.AddDays(10).AddHours(10);

    private readonly BaseDeDatosPrueba _bd = new();
    private readonly UsuarioRepositorio _usuarios;
    private readonly EntrenadorRepositorio _entrenadores;
    private readonly Sala _sala;
    private readonly Actividad _yoga;
    private readonly Entrenador _laura;
    private readonly List<Usuario> _clientes;

    public UsuariosTests()
    {
        var db = _bd.Contexto;
        _usuarios = new UsuarioRepositorio(db);
        _entrenadores = new EntrenadorRepositorio(db);
        _sala = DatosAgenda.CrearSala(db, "Sala Grande");
        _yoga = DatosAgenda.CrearActividad(db, "Yoga", plazas: 4);          // especialidad Yoga
        _laura = DatosAgenda.CrearEntrenador(db, "laura");                   // especialidad Yoga
        _clientes = DatosPrueba.CrearUsuarios(_usuarios, 4, Rol.Cliente, "cli");
    }

    public void Dispose() => _bd.Dispose();

    private static Usuario Datos(Usuario u, string? dni = null, Rol? rol = null) => new()
    {
        Nombre = "Nombre nuevo",
        Apellido1 = u.Apellido1,
        Dni = dni ?? u.Dni,
        Email = u.Email,
        Telefono = u.Telefono,
        Rol = rol ?? u.Rol
    };

    // ---------- Modificar ----------

    [Fact]
    public void Modificar_guarda_los_datos_nuevos()
    {
        _usuarios.Modificar(_clientes[0].UsuarioId, Datos(_clientes[0]));

        using var otro = _bd.NuevoContexto();
        Assert.Equal("Nombre nuevo", otro.Usuarios.Single(u => u.UsuarioId == _clientes[0].UsuarioId).Nombre);
    }

    [Fact]
    public void Modificar_con_el_DNI_de_otro_usuario_lanza_ValidacionException_y_no_guarda_nada()
    {
        var ex = Assert.Throws<ValidacionException>(() =>
            _usuarios.Modificar(_clientes[0].UsuarioId, Datos(_clientes[0], dni: _clientes[1].Dni)));

        Assert.Equal("Ya existe un usuario con ese DNI.", ex.Message);
        using var otro = _bd.NuevoContexto();
        Assert.Equal("Ana", otro.Usuarios.Single(u => u.UsuarioId == _clientes[0].UsuarioId).Nombre);
    }

    [Fact]
    public void Un_entrenador_no_puede_cambiar_de_rol_ni_nadie_convertirse_en_entrenador()
    {
        var lauraUsuario = _usuarios.ObtenerPorId(_laura.EntrenadorId)!;

        Assert.Throws<ValidacionException>(() =>
            _usuarios.Modificar(_laura.EntrenadorId, Datos(lauraUsuario, rol: Rol.Cliente)));
        Assert.Throws<ValidacionException>(() =>
            _usuarios.Modificar(_clientes[0].UsuarioId, Datos(_clientes[0], rol: Rol.Entrenador)));
    }

    [Fact]
    public void El_administrador_puede_cambiar_el_rol_de_un_cliente_a_recepcionista()
    {
        _usuarios.Modificar(_clientes[0].UsuarioId, Datos(_clientes[0], rol: Rol.Recepcionista));

        using var otro = _bd.NuevoContexto();
        Assert.Equal(Rol.Recepcionista, otro.Usuarios.Single(u => u.UsuarioId == _clientes[0].UsuarioId).Rol);
    }

    [Fact]
    public void Cambiar_la_especialidad_con_sesiones_futuras_de_otra_lanza_ValidacionException()
    {
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, Futuro);
        var lauraUsuario = _usuarios.ObtenerPorId(_laura.EntrenadorId)!;

        var ex = Assert.Throws<ValidacionException>(() =>
            _usuarios.Modificar(_laura.EntrenadorId, Datos(lauraUsuario), Especialidad.Pilates));

        Assert.Contains("Yoga", ex.Message);
    }

    [Fact]
    public void Cambiar_la_especialidad_sin_sesiones_futuras_se_guarda()
    {
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, Desde.AddHours(10));   // ya pasada
        var lauraUsuario = _usuarios.ObtenerPorId(_laura.EntrenadorId)!;

        _usuarios.Modificar(_laura.EntrenadorId, Datos(lauraUsuario), Especialidad.Pilates);

        using var otro = _bd.NuevoContexto();
        Assert.Equal(Especialidad.Pilates, otro.Entrenadores.Single(e => e.EntrenadorId == _laura.EntrenadorId).Especialidad);
    }

    // ---------- Baja y reactivar ----------

    [Fact]
    public void Dar_de_baja_a_un_entrenador_con_sesiones_programadas_lanza_ValidacionException()
    {
        DatosAgenda.CrearSesion(_bd.Contexto, _yoga, _sala, _laura, Futuro);

        var ex = Assert.Throws<ValidacionException>(() => _entrenadores.DarDeBaja(_laura.EntrenadorId));

        Assert.StartsWith("Tiene 1 sesión programada", ex.Message);
        Assert.True(_usuarios.ObtenerPorId(_laura.EntrenadorId)!.Activo);
    }

    [Fact]
    public void Dar_de_baja_a_un_entrenador_cancela_sus_citas_futuras()
    {
        var cita = DatosAgenda.CrearCita(_bd.Contexto, _clientes[0], _laura, Futuro);

        _entrenadores.DarDeBaja(_laura.EntrenadorId);

        using var otro = _bd.NuevoContexto();
        Assert.False(otro.Usuarios.Single(u => u.UsuarioId == _laura.EntrenadorId).Activo);
        Assert.Equal(EstadoCita.Cancelada, otro.Citas.Single(c => c.CitaId == cita.CitaId).Estado);
    }

    [Fact]
    public void Reactivar_devuelve_el_acceso_y_no_se_puede_dar_de_baja_dos_veces()
    {
        var ana = _clientes[0];
        _usuarios.DarDeBaja(ana.UsuarioId);
        Assert.Throws<ValidacionException>(() => _usuarios.DarDeBaja(ana.UsuarioId));

        _usuarios.Reactivar(ana.UsuarioId);

        Assert.Equal(ana.UsuarioId, _usuarios.IniciarSesion(ana.Email, DatosPrueba.Contrasena).UsuarioId);
        Assert.Throws<ValidacionException>(() => _usuarios.Reactivar(ana.UsuarioId));
    }

    // ---------- Informe de usuarios por rol (administrador) ----------

    [Fact]
    public void InformePorRol_sale_un_renglon_por_rol_con_activos_y_de_baja()
    {
        _usuarios.DarDeBaja(_clientes[3].UsuarioId);

        var filas = _usuarios.InformePorRol();

        Assert.Equal(Enum.GetValues<Rol>().Length, filas.Count);
        var clientes = filas.Single(f => f.Rol == Rol.Cliente);
        Assert.Equal(3, clientes.Activos);
        Assert.Equal(1, clientes.DeBaja);
        Assert.Equal(1, filas.Single(f => f.Rol == Rol.Entrenador).Total);
        Assert.Equal(0, filas.Single(f => f.Rol == Rol.Administrador).Total);
    }

    // ---------- Informe de carga de entrenadores (encargado) ----------

    [Fact]
    public void InformeCarga_suma_sesiones_y_citas_y_ordena_de_mas_a_menos_horas()
    {
        var db = _bd.Contexto;
        var marc = DatosAgenda.CrearEntrenador(db, "marc");
        var s1 = DatosAgenda.CrearSesion(db, _yoga, _sala, _laura, Desde.AddDays(2).AddHours(10));   // 60 min
        DatosAgenda.CrearSesion(db, _yoga, _sala, _laura, Desde.AddDays(3).AddHours(10), EstadoSesion.Cancelada);
        DatosAgenda.CrearReservas(db, s1, _clientes.Take(3), EstadoReserva.Confirmada);                // 3 de 4
        DatosAgenda.CrearCita(db, _clientes[0], marc, Desde.AddDays(4).AddHours(9));                   // 30 min
        DatosAgenda.CrearCita(db, _clientes[1], marc, Desde.AddDays(4).AddHours(10), EstadoCita.Cancelada);

        var informe = _entrenadores.InformeCarga(Desde, Hasta);

        Assert.Equal(["laura", "marc"], informe.Filas.Select(f => f.Entrenador.Usuario.Dni));
        var laura = informe.Filas[0];
        Assert.Equal(1, laura.Sesiones);
        Assert.Equal(1.0, laura.Horas);
        Assert.Equal(75, laura.Ocupacion);
        var marcFila = informe.Filas[1];
        Assert.Equal(1, marcFila.Citas);
        Assert.Equal(0.5, marcFila.Horas);
        Assert.Null(marcFila.Ocupacion);
        Assert.Equal(1.5, informe.TotalHoras);
        Assert.Equal(0, informe.SinActividad);
    }

    [Fact]
    public void InformeCarga_no_incluye_entrenadores_de_baja_y_cuenta_los_que_no_trabajan()
    {
        var marc = DatosAgenda.CrearEntrenador(_bd.Contexto, "marc");
        DatosAgenda.CrearEntrenador(_bd.Contexto, "sara");
        _entrenadores.DarDeBaja(marc.EntrenadorId);

        var informe = _entrenadores.InformeCarga(Desde, Hasta);

        Assert.Equal(2, informe.Filas.Count);
        Assert.Equal(2, informe.SinActividad);
        Assert.Null(informe.MasCargado);
    }

    [Fact]
    public void InformeCarga_con_las_fechas_al_reves_lanza_ValidacionException()
    {
        Assert.Throws<ValidacionException>(() => _entrenadores.InformeCarga(Hasta, Desde));
    }
}
