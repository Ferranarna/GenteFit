using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenteFit.Modelo.Entidades;

namespace GenteFit.Datos.Repositorios;

public record MiReserva(Reserva Reserva, int? Posicion);
