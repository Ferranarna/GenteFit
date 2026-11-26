using GenteFit.Datos.Repositorios;
using GenteFit.Xml;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Web.Helpers;

// Traduce las entidades de GenteFit (EF Core) a los tipos del lote de Odoo (F#).
public static class MapeoOdoo
{
    public static SesionLote ASesionLote(SesionParaExportar fila)
    {
        var s = fila.Sesion;
        var u = s.Entrenador.Usuario;

        return new SesionLote(
            gentefitId: s.SesionId,
            fecha: s.Fecha,
            duracion: s.Duracion,
            estado: s.Estado,
            actividad: new Referencia(s.ActividadId, s.Actividad.NombreActividad),
            intensidad: s.Actividad.Intensidad,
            sala: new Referencia(s.SalaId, s.Sala.NombreSala),
            entrenador: new Referencia(s.EntrenadorId, $"{u.Nombre} {u.Apellido1}"),
            plazasOcupadas: fila.Ocupadas,
            plazasLimite: s.Actividad.LimiteUsuarios);
    }

    public static Sesion ASesion(SesionRecibida r) => new()
    {
        SesionId = r.GentefitId,
        Fecha = r.Fecha,
        Duracion = r.Duracion,
        Estado = r.Estado,
        ActividadId = r.Actividad.Id,
        SalaId = r.Sala.Id,
        EntrenadorId = r.Entrenador.Id
    };
}