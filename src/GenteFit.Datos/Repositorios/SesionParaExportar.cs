using GenteFit.Modelo.Entidades;

namespace GenteFit.Datos.Repositorios;

// Una sesión con sus plazas ocupadas (reservas confirmadas), lista para enviar a Odoo.
public record SesionParaExportar(Sesion Sesion, int Ocupadas);