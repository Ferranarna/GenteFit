using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

namespace GenteFit.Datos.Repositorios;
public record SesionDelDia(Sesion Sesion, int Confirmadas, int EnEspera, EstadoReserva? MiEstado, int? MiPosicion)
{
    public int Plazas => Sesion.Actividad.LimiteUsuarios;
    public bool Completa => Confirmadas >= Plazas;
}