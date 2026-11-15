using System;
using GenteFit.Datos.Seguridad;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

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

    public Usuario ModificarPerfil(Guid usuarioId, Usuario datos)
    {
        var usuario = ObtenerPorId(usuarioId) ?? throw new ValidacionException("El usuario no existe.");

        Validador.Obligatorio(datos.Nombre, "nombre");
        Validador.Obligatorio(datos.Apellido1, "primer apellido");
        Validador.Email(datos.Email);
        Validador.Telefono(datos.Telefono);
        if (_db.Usuarios.Any(u => u.Email == datos.Email && u.UsuarioId != usuarioId))
            throw new ValidacionException("Ya existe un usuario con ese email.");

        usuario.Nombre = datos.Nombre;
        usuario.Apellido1 = datos.Apellido1;
        usuario.Apellido2 = datos.Apellido2;
        usuario.Email = datos.Email;
        usuario.Telefono = datos.Telefono;

        _db.SaveChanges();
        return usuario;
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
        ComprobarCambioDeRol(usuario.Rol, rol);
        usuario.Rol = rol;
        _db.SaveChanges();
    }

    // Un entrenador tiene además su ficha (especialidad, sesiones y citas), así que no se cambia de rol:
    // se da de baja y se crea el usuario nuevo. Por eso tampoco se puede convertir a nadie en entrenador.
    private static void ComprobarCambioDeRol(Rol actual, Rol nuevo)
    {
        if (actual == nuevo)
            return;
        if (actual == Rol.Entrenador)
            throw new ValidacionException("Un entrenador no puede cambiar de rol. Dalo de baja y crea un usuario nuevo.");
        if (nuevo == Rol.Entrenador)
            throw new ValidacionException("Para tener un entrenador hay que crearlo nuevo con su especialidad.");
    }

    // Gestión de usuarios: el personal modifica los datos de otro usuario (y el administrador, su rol).
    // Todo se comprueba antes de guardar, así un error no deja nada a medias.
    public Usuario Modificar(Guid usuarioId, Usuario datos, Especialidad? especialidad = null)
    {
        var usuario = _db.Usuarios.Include(u => u.Entrenador).FirstOrDefault(u => u.UsuarioId == usuarioId)
            ?? throw new ValidacionException("El usuario no existe.");

        datos.UsuarioId = usuarioId;
        Validar(datos);
        ComprobarCambioDeRol(usuario.Rol, datos.Rol);

        if (usuario.Entrenador != null && especialidad != null && especialidad != usuario.Entrenador.Especialidad)
        {
            var otra = _db.Sesiones
                .Where(s => s.EntrenadorId == usuarioId && s.Estado == EstadoSesion.Programada
                    && s.Fecha > DateTime.Now && s.Actividad.Especialidad != especialidad)
                .Select(s => s.Actividad.NombreActividad)
                .FirstOrDefault();
            if (otra != null)
                throw new ValidacionException($"Tiene sesiones de {otra} programadas. Cámbialas de entrenador antes de cambiar su especialidad.");

            usuario.Entrenador.Especialidad = especialidad.Value;
        }

        usuario.Nombre = datos.Nombre.Trim();
        usuario.Apellido1 = datos.Apellido1.Trim();
        usuario.Apellido2 = string.IsNullOrWhiteSpace(datos.Apellido2) ? null : datos.Apellido2.Trim();
        usuario.Dni = datos.Dni.Trim().ToUpperInvariant();
        usuario.Email = datos.Email.Trim();
        usuario.Telefono = datos.Telefono;
        usuario.Rol = datos.Rol;

        _db.SaveChanges();
        return usuario;
    }

    // Vuelve a dar de alta a un usuario que estaba de baja (puede iniciar sesión otra vez)
    public void Reactivar(Guid usuarioId)
    {
        var usuario = ObtenerPorId(usuarioId) ?? throw new ValidacionException("El usuario no existe.");
        if (usuario.Activo)
            throw new ValidacionException("El usuario ya está activo.");
        usuario.Activo = true;
        _db.SaveChanges();
    }

    public void DarDeBaja(Guid usuarioId)
    {
        var usuario = ObtenerPorId(usuarioId) ?? throw new ValidacionException("El usuario no existe.");
        if (!usuario.Activo)
            throw new ValidacionException("El usuario ya está de baja.");

        // Un entrenador con clases por delante no se puede ir sin más: alguien tiene que darlas
        if (usuario.Rol == Rol.Entrenador)
        {
            var pendientes = _db.Sesiones.Count(s => s.EntrenadorId == usuarioId
                && s.Estado == EstadoSesion.Programada && s.Fecha > DateTime.Now);
            if (pendientes > 0)
                throw new ValidacionException($"Tiene {pendientes} {(pendientes == 1 ? "sesión programada" : "sesiones programadas")}. Cámbialas de entrenador o cancélalas antes de darlo de baja.");
        }

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
            .Where(c => (c.UsuarioId == usuarioId || c.EntrenadorId == usuarioId)
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

    // Estadística del inicio del administrador: usuarios activos y de baja
    public (int Activos, int DeBaja) ContarActivosYDeBaja() =>
        (_db.Usuarios.Count(u => u.Activo), _db.Usuarios.Count(u => !u.Activo));

    // Estadística del inicio del administrador: clientes activos y personal activo (el resto de roles)
    public (int Clientes, int Plantilla) ContarClientesYPlantilla() =>
        (_db.Usuarios.Count(u => u.Activo && u.Rol == Rol.Cliente),
         _db.Usuarios.Count(u => u.Activo && u.Rol != Rol.Cliente));

    // Estadística del inicio del recepcionista: clientes activos y de baja
    public (int Activos, int DeBaja) ContarClientesActivosYDeBaja() =>
        (_db.Usuarios.Count(u => u.Rol == Rol.Cliente && u.Activo),
         _db.Usuarios.Count(u => u.Rol == Rol.Cliente && !u.Activo));
    // Informe del administrador: cuántos usuarios hay de cada rol, activos y de baja.
    // Salen todos los roles, también los que no tienen a nadie.
    public List<RolDelInforme> InformePorRol()
    {
        var usuarios = _db.Usuarios.Select(u => new { u.Rol, u.Activo }).ToList();
        return Enum.GetValues<Rol>()
            .Select(rol => new RolDelInforme(rol,
                usuarios.Count(u => u.Rol == rol && u.Activo),
                usuarios.Count(u => u.Rol == rol && !u.Activo)))
            .ToList();
    }

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