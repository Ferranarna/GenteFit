using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

public record SesionDelInforme(Sesion Sesion, int Confirmadas)
{
    public int Plazas => Sesion.Actividad.LimiteUsuarios;

    // Las canceladas no tienen ocupación (sus reservas están anuladas)
    public int? Porcentaje => Sesion.Estado == EstadoSesion.Cancelada || Plazas == 0
        ? null
        : Confirmadas * 100 / Plazas;
}

// El informe completo: las filas y los totales de las tarjetas
public record InformeSesiones(List<SesionDelInforme> Filas)
{
    public int Programadas => Filas.Count(f => f.Sesion.Estado == EstadoSesion.Programada);
    public int Canceladas => Filas.Count(f => f.Sesion.Estado == EstadoSesion.Cancelada);

    // Plazas reservadas frente a plazas totales de las programadas (null si no hay ninguna)
    public int? OcupacionMedia
    {
        get
        {
            var programadas = Filas.Where(f => f.Sesion.Estado == EstadoSesion.Programada).ToList();
            var plazas = programadas.Sum(f => f.Plazas);
            return plazas == 0 ? null : programadas.Sum(f => f.Confirmadas) * 100 / plazas;
        }
    }
}