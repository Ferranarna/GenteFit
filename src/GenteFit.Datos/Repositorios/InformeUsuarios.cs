using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;

// ---------- Informe del administrador: usuarios por rol ----------

public record RolDelInforme(Rol Rol, int Activos, int DeBaja)
{
    public int Total => Activos + DeBaja;
}

// ---------- Informe del encargado: carga de trabajo de los entrenadores ----------

// Una fila: lo que ha trabajado un entrenador en el periodo
public record EntrenadorDelInforme(Entrenador Entrenador, int Sesiones, int MinutosSesiones,
    int Confirmadas, int Plazas, int Citas, int MinutosCitas)
{
    public double Horas => (MinutosSesiones + MinutosCitas) / 60.0;

    // Plazas ocupadas en sus sesiones, en % (null si no ha tenido sesiones)
    public int? Ocupacion => Plazas == 0 ? null : Confirmadas * 100 / Plazas;
}

// El informe completo: filas de más a menos horas y los datos de las tarjetas
public record InformeEntrenadores(List<EntrenadorDelInforme> Filas)
{
    public double TotalHoras => Filas.Sum(f => f.Horas);

    // Horas medias por entrenador activo
    public double MediaHoras => Filas.Count == 0 ? 0 : TotalHoras / Filas.Count;

    // El que más horas ha trabajado (null si nadie ha trabajado)
    public EntrenadorDelInforme? MasCargado => Filas.FirstOrDefault(f => f.Horas > 0);

    // Entrenadores activos sin ninguna sesión ni cita en el periodo
    public int SinActividad => Filas.Count(f => f.Horas == 0);
}
