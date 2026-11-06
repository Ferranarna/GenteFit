using GenteFit.Modelo.Enums;
namespace GenteFit.Web.Models
{
    public class SesionCalendarViewModel
    {
        public Guid SesionId { get; set; }

        public DateTime Inicio { get; set; }
        public int DuracionMinutos { get; set; }
        public DateTime Fin => Inicio.AddMinutes(DuracionMinutos);

        public string Actividad {  get; set; } = string.Empty;
        public Intensidad Intensidad { get; set; }
        public string Sala { get; set; } = string.Empty;
        public string Monitor { get; set; } = string.Empty;

        public int PlazasTotales { get; set; }
        public int PlazasLibres { get; set; }
        public bool Completa => PlazasLibres == 0;
    }
}
