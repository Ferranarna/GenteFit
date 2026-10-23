using System.Security.Cryptography;

namespace GenteFit.Datos.Seguridad;


public static class HashContrasena
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public static string Generar(string contrasena)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string contrasena, string guardado)
    {
        string[] partes = guardado.Split('.');
        if (partes.Length != 3) return false;

        int iteraciones = int.Parse(partes[0]);
        byte[] sal = Convert.FromBase64String(partes[1]);
        byte[] hashGuardado = Convert.FromBase64String(partes[2]);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, hashGuardado.Length);
        return CryptographicOperations.FixedTimeEquals(hash, hashGuardado);
    }
}