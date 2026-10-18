using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos;

public class GenteFitContext : DbContext
{
        // Constructor normal
    public GenteFitContext() { }

    // Constructor para pruebas o para pasarle otra configuración desde fuera
    public GenteFitContext(DbContextOptions<GenteFitContext> options) : base(options) { }
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Entrenador> Entrenadores => Set<Entrenador>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Rutina> Rutinas => Set<Rutina>();
    public DbSet<DetalleRutina> DetallesRutina => Set<DetalleRutina>();
    public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=GenteFit;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Rol>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<Especialidad>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<Intensidad>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<Ubicacion>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<EstadoReserva>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<EstadoSesion>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<EstadoCita>().HaveConversion<string>().HaveMaxLength(30);

        configurationBuilder.Properties<string>().HaveMaxLength(255);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Dni).IsUnique();

        modelBuilder.Entity<Reserva>().HasIndex(r => new { r.UsuarioId, r.SesionId }).IsUnique();

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<DetalleRutina>()
            .HasOne(d => d.Rutina)
            .WithMany(r => r.Detalles)
            .HasForeignKey(d => d.RutinaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}