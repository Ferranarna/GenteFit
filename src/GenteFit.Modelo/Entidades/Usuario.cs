using GenteFit.Modelo.Enums;

namespace GenteFit.Modelo.Entidades;

public class Usuario
{
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido1 { get; set; } = string.Empty;
    public string? Apellido2 { get; set; }
    public string Dni { get; set; } = string.Empty;
    public Rol Rol { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty; // se guarda el hash, nunca la contraseña tal cual
    public bool Activo { get; set; } = true;
    public int Telefono { get; set; }


    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<Rutina> Rutinas { get; set; } = new List<Rutina>();
}