using System;
using GenteFit.Datos.Seguridad;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

// Sirve para todos los roles 
public class UsuarioRepositorio : Repositorio<Usuario>
{
    public UsuarioRepositorio(GenteFitContext db) : base(db) { }

    public Usuario Crear(Usuario usuario, string contrasena)
    {
        Validar(usuario);
        Validador.Obligatorio(contrasena, "contraseña");

        usuario.Contrasena = HashContrasena.Generar(contrasena);
        usuario.Activo = true;
        return base.Crear(usuario);
    }

    public override void Actualizar(Usuario usuario)
    {
        Validar(usuario);
        base.Actualizar(usuario);
    }

    public Usuario IniciarSesion(string email, string contrasena)
    {
        var usuario = _db.Usuarios.FirstOrDefault(u => u.Email == email && u.Activo);
        if (usuario == null || !HashContrasena.Verificar(contrasena, usuario.Contrasena))
            throw new ValidacionException("Email o contraseña incorrectos.");
        return usuario;
    }

    public void CambiarContrasena(Guid usuarioId, string actual, string nueva)
    {
        var usuario = ObtenerPorId(usuarioId) ?? throw new ValidacionException("El usuario no existe.");
        if (!HashContrasena.Verificar(actual, usuario.Contrasena))
            throw new ValidacionException("La contraseña actual no es correcta.");
        Validador.Obligatorio(nueva, "nueva contraseña");

        usuario.Contrasena = HashContrasena.Generar(nueva);
        _db.SaveChanges();
    }

    public void CambiarRol(Guid usuarioId, Rol rol)
    {
        var usuario = ObtenerPorId(usuarioId) ?? throw new ValidacionException("El usuario no existe.");
        usuario.Rol = rol;
        _db.SaveChanges();
    }

    public void DarDeBaja(Guid usuarioId)
    {
        var usuario = ObtenerPorId(usuarioId) ?? throw new ValidacionException("El usuario no existe.");

        using var transaccion = _db.Database.BeginTransaction();
        AnularReservasYCitas(usuarioId);
        usuario.Activo = false;
        _db.SaveChanges();
        transaccion.Commit();
    }

    private void AnularReservasYCitas(Guid usuarioId)
    {
        var reservas = new ReservaRepositorio(_db);
        var pendientes = _db.Reservas
            .Where(r => r.UsuarioId == usuarioId
                && r.Estado != EstadoReserva.Cancelada
                && r.Sesion.Estado == EstadoSesion.Programada)
            .ToList();
        foreach (var reserva in pendientes)
            reservas.Anular(reserva.ReservaId);

        var citasFuturas = _db.Citas
            .Where(c => c.UsuarioId == usuarioId
                && c.Estado == EstadoCita.Programada
                && c.FechaActividad > DateTime.Now)
            .ToList();
        foreach (var cita in citasFuturas)
            cita.Estado = EstadoCita.Cancelada;
    }

    public List<Usuario> Buscar(string texto, Rol? rol = null)
    {
        var consulta = _db.Usuarios.Where(u =>
            u.Nombre.Contains(texto) || u.Apellido1.Contains(texto) ||
            u.Dni.Contains(texto) || u.Email.Contains(texto));

        if (rol != null)
            consulta = consulta.Where(u => u.Rol == rol);

        return consulta.OrderBy(u => u.Nombre).ToList();
    }

    public List<Usuario> ObtenerClientes() =>
        _db.Usuarios.Where(u => u.Rol == Rol.Cliente).OrderBy(u => u.Nombre).ToList();

    private void Validar(Usuario usuario)
    {
        Validador.Obligatorio(usuario.Nombre, "nombre");
        Validador.Obligatorio(usuario.Apellido1, "primer apellido");
        Validador.Obligatorio(usuario.Dni, "DNI");
        Validador.Email(usuario.Email);
        Validador.Telefono(usuario.Telefono);

        if (_db.Usuarios.Any(u => u.Dni == usuario.Dni && u.UsuarioId != usuario.UsuarioId))
            throw new ValidacionException("Ya existe un usuario con ese DNI.");
        if (_db.Usuarios.Any(u => u.Email == usuario.Email && u.UsuarioId != usuario.UsuarioId))
            throw new ValidacionException("Ya existe un usuario con ese email.");
    }
}