using System;

namespace GenteFit.Datos.Validaciones;

// Comprobaciones comunes de los "casos de error"
public static class Validador
{
    public static void Obligatorio(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ValidacionException($"El campo {campo} es obligatorio.");
    }

    public static void Email(string email)
    {
        Obligatorio(email, "email");
        if (!email.Contains('@') || !email.Contains('.'))
            throw new ValidacionException("El email no es válido.");
    }

    public static void Telefono(int telefono)
    {
        
        if (telefono < 100_000_000 || telefono > 999_999_999)
            throw new ValidacionException("El teléfono debe tener 9 cifras.");
    }

    public static void MayorQueCero(int valor, string campo)
    {
        if (valor <= 0)
            throw new ValidacionException($"El campo {campo} debe ser mayor que 0.");
    }
}
