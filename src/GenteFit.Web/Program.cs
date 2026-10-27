using GenteFit.Datos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

string cadena = builder.Configuration.GetConnectionString("GenteFit")
    ?? throw new InvalidOperationException("Falta la cadena de conexión GenteFit.");
builder.Services.AddDbContext<GenteFitContext>(o => o.UseSqlServer(cadena));

var app = builder.Build();

// Crea la BD y los datos de prueba al arrancar. Si no hay conexión, la aplicación arranca igualmente y la página principal muestra el aviso.
using (var scope = app.Services.CreateScope())
{
    try { InicializadorBD.Inicializar(scope.ServiceProvider.GetRequiredService<GenteFitContext>()); }
    catch (Exception) { }
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();