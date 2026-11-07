using GenteFit.Datos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Tests;

// Base de datos en memoria para los tests: se crea vacía y desaparece al terminar
public class BaseDeDatosPrueba : IDisposable
{
    private readonly SqliteConnection _conexion;

    public GenteFitContext Contexto { get; }

    public BaseDeDatosPrueba()
    {
        // La base en memoria solo vive mientras la conexión esté abierta
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<GenteFitContext>()
            .UseSqlite(_conexion)
            .Options;

        Contexto = new GenteFitContext(opciones);
        Contexto.Database.EnsureCreated();   // crea las tablas a partir de las clases del modelo
    }

    public void Dispose()
    {
        Contexto.Dispose();
        _conexion.Dispose();
    }
}