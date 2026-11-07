using Microsoft.Data.SqlClient;

namespace GenteFit.Web.Helpers;

// Ayuda común para manejar errores en los controladores
public static class Errores
{
    // Mensaje que se muestra cuando falla la base de datos
    public const string ErrorBaseDeDatos =
        "Hay un problema con la base de datos. Comprueba que SQL Server está en marcha e inténtalo de nuevo.";

    // ¿El fallo viene de la base de datos? Mira la excepción y las que lleva dentro
    public static bool EsDeBaseDeDatos(Exception ex)
    {
        for (var actual = ex; actual != null; actual = actual.InnerException)
            if (actual is SqlException) return true;
        return false;
    }
}