using GenteFit.Modelo.Entidades;

namespace GenteFit.Datos.Repositorios;

// Una fila del informe: lo que ha tenido una actividad en el periodo
public record ActividadDelInforme(Actividad Actividad, int Sesiones, int Confirmadas, int EnEspera)
{
    // Plazas que se han ofrecido: sesiones programadas x plazas de la actividad
    public int Plazas => Sesiones * Actividad.LimiteUsuarios;

    // Ocupación en %, o null si la actividad no ha tenido sesiones
    public int? Porcentaje => Plazas == 0 ? null : Confirmadas * 100 / Plazas;
}

// El informe completo: las filas (de más a menos reservada) y los totales de las tarjetas
public record InformeActividades(List<ActividadDelInforme> Filas)
{
    public int TotalReservas => Filas.Sum(f => f.Confirmadas);

    public int TotalEnEspera => Filas.Sum(f => f.EnEspera);

    // La primera fila con alguna reserva (null si no hay ninguna en el periodo)
    public ActividadDelInforme? MasReservada => Filas.FirstOrDefault(f => f.Confirmadas > 0);

    // Plazas reservadas frente a plazas ofrecidas de todas las actividades (null si no hubo sesiones)
    public int? OcupacionMedia
    {
        get
        {
            var plazas = Filas.Sum(f => f.Plazas);
            return plazas == 0 ? null : TotalReservas * 100 / plazas;
        }
    }
}
