using GenteFit.Datos.Repositorios;

namespace GenteFit.Web.Models;

// Datos de la pantalla del informe de actividades más reservadas
public class InformeActividadesViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }

    // null si no se ha podido calcular (fechas mal o BD caída)
    public InformeActividades? Informe { get; set; }
}
