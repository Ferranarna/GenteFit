using System;

namespace GenteFit.Datos.Validaciones;

// Error de datos introducidos por el usuario 
public class ValidacionException : Exception
{
    public ValidacionException(string mensaje) : base(mensaje) { }
}
