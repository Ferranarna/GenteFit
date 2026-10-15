using GenteFit.Modelo.Enums;

namespace GenteFit.Modelo.Entidades;

public class Sala
{
    public Guid SalaId { get; set; }
    public string NombreSala { get; set; } = string.Empty;
    public Ubicacion Ubicacion { get; set; }

    
    public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
}