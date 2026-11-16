using GenteFit.Modelo.Entidades;

namespace GenteFit.Datos.Repositorios;

// Una fila del informe: el uso de una sala en el periodo
public record SalaDelInforme(Sala Sala, int Sesiones, int Minutos, int Asistentes, int MinutosDisponibles)
{
    public double Horas => Minutos / 60.0;

    // Parte de la jornada del centro en la que la sala ha estado ocupada, en %
    public int Porcentaje => MinutosDisponibles == 0 ? 0 : Minutos * 100 / MinutosDisponibles;

    // Clientes con plaza confirmada por sesión (null si no ha tenido sesiones)
    public double? MediaAsistentes => Sesiones == 0 ? null : Math.Round((double)Asistentes / Sesiones, 1);
}

// El informe completo: las filas (de más a menos uso) y los totales de las tarjetas
public record InformeSalas(List<SalaDelInforme> Filas, int Dias)
{
    public int TotalSesiones => Filas.Sum(f => f.Sesiones);

    public double TotalHoras => Filas.Sum(f => f.Horas);

    // La sala con más horas de uso (null si ninguna ha tenido sesiones)
    public SalaDelInforme? MasUsada => Filas.FirstOrDefault(f => f.Sesiones > 0);

    // Salas que no han tenido ninguna sesión en el periodo
    public int SinUso => Filas.Count(f => f.Sesiones == 0);
}
