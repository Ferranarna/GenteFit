using GenteFit.Datos;
using GenteFit.Datos.Repositorios;
using GenteFit.Datos.Validaciones;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;

// Programa de demostración de la capa de datos.

using var db = new GenteFitContext();

Titulo("1. Inicializar la base de datos");
InicializadorBD.Inicializar(db);
Ok("Base de datos lista.");

var usuarios = new UsuarioRepositorio(db);
var actividades = new ActividadRepositorio(db);
var entrenadores = new EntrenadorRepositorio(db);
var salas = new SalaRepositorio(db);
var sesiones = new SesionRepositorio(db);
var reservas = new ReservaRepositorio(db);
var citas = new CitaRepositorio(db);
var rutinas = new RutinaRepositorio(db);
var ejercicios = new EjercicioRepositorio(db);

Titulo("2. Inicio de sesión");
var admin = usuarios.IniciarSesion("admin@gentefit.com", InicializadorBD.ContrasenaPrueba);
Ok($"Sesión iniciada: {admin.Nombre} ({admin.Rol})");
Probar("Contraseña incorrecta", () => usuarios.IniciarSesion("admin@gentefit.com", "mala"));

Titulo("3. Catálogo y sesiones de mañana");
foreach (var a in actividades.ObtenerTodos())
    Console.WriteLine($"  - {a.NombreActividad} ({a.Intensidad}, máx. {a.LimiteUsuarios})");
foreach (var s in sesiones.Buscar(null, DateTime.Today.AddDays(1)))
    Console.WriteLine($"  - {s.Fecha:dd/MM HH:mm} {s.Actividad.NombreActividad} con {s.Entrenador.Usuario.Nombre} en {s.Sala.NombreSala} · plazas libres: {sesiones.PlazasDisponibles(s.SesionId)}");

Titulo("4. Validaciones al crear datos");
var yoga = actividades.Buscar("Yoga").First();
var salaGrande = salas.Buscar("Grande").First();
var instructoraYoga = entrenadores.ObtenerActivosPorEspecialidad(Especialidad.Yoga).First();
var instructorSpinning = entrenadores.ObtenerActivosPorEspecialidad(Especialidad.Spinning).First();
var manana = DateTime.Today.AddDays(1);

Probar("Sesión con entrenador de otra especialidad", () => sesiones.Crear(new Sesion
{
    ActividadId = yoga.ActividadId,
    SalaId = salaGrande.SalaId,
    EntrenadorId = instructorSpinning.EntrenadorId,
    Fecha = manana.AddHours(21),
    Duracion = 60
}));
Probar("Sesión fuera de horario", () => sesiones.Crear(new Sesion
{
    ActividadId = yoga.ActividadId,
    SalaId = salaGrande.SalaId,
    EntrenadorId = instructoraYoga.EntrenadorId,
    Fecha = manana.AddHours(7),
    Duracion = 60
}));
Probar("Cliente con email incorrecto", () => usuarios.Crear(new Usuario
{
    Nombre = "Pepe",
    Apellido1 = "Mal",
    Dni = "99999999Z",
    Email = "pepe-sin-arroba",
    Telefono = 699000000,
    Rol = Rol.Cliente
}, "1234"));
Probar("Cliente con DNI repetido", () => usuarios.Crear(new Usuario
{
    Nombre = "Otra",
    Apellido1 = "Ana",
    Dni = "11111111A",
    Email = "otra@correo.com",
    Telefono = 699000001,
    Rol = Rol.Cliente
}, "1234"));

Titulo("5. Reservas y lista de espera");
var sesionDemo = sesiones.Crear(new Sesion
{
    ActividadId = yoga.ActividadId,
    SalaId = salaGrande.SalaId,
    EntrenadorId = instructoraYoga.EntrenadorId,
    Fecha = manana.AddHours(21),
    Duracion = 60
});
Ok($"Sesión de demo creada: {yoga.NombreActividad} {sesionDemo.Fecha:dd/MM HH:mm}");

// 18 clientes de prueba: 16 cogen plaza y 2 van a la lista de espera
var clientesDemo = ObtenerClientesDemo(18);
foreach (var cliente in clientesDemo)
{
    var r = reservas.Reservar(cliente.UsuarioId, sesionDemo.SesionId);
    Console.WriteLine($"  {cliente.Nombre,-10} -> {r.Estado}");
}
MostrarEstado(sesionDemo.SesionId);

Probar("Reservar dos veces la misma sesión", () => reservas.Reservar(clientesDemo[0].UsuarioId, sesionDemo.SesionId));

Titulo("6. Anular una reserva con plaza -> promoción automática");
var primera = reservas.ListarConfirmados(sesionDemo.SesionId).First();
var promovida = reservas.Anular(primera.ReservaId);
Ok($"{primera.Usuario.Nombre} anula su reserva.");
if (promovida != null)
    Ok($"{promovida.Usuario.Nombre} pasa de la lista de espera a CONFIRMADA (se le avisaría por correo).");
MostrarEstado(sesionDemo.SesionId);

Titulo("7. Cancelar la sesión");
var afectados = sesiones.Cancelar(sesionDemo.SesionId);
Ok($"Sesión cancelada. Clientes a avisar por correo: {afectados.Count}");

Titulo("8. Citas con entrenador personal");
var personal = entrenadores.ObtenerActivosPorEspecialidad(Especialidad.EntrenadorPersonal).First();
var ana = usuarios.Buscar("ana@correo.com").First();
var huecos = citas.BuscarDisponibilidad(personal.EntrenadorId, manana.AddDays(1));
var cita = citas.Reservar(ana.UsuarioId, personal.EntrenadorId, huecos.First());
Ok($"Cita de {ana.Nombre} con {personal.Usuario.Nombre} el {cita.FechaActividad:dd/MM HH:mm} ({cita.Duracion} min)");
Probar("Otra cita a la misma hora", () => citas.Reservar(ana.UsuarioId, personal.EntrenadorId, cita.FechaActividad));
Probar("Cita con entrenador que no es personal", () => citas.Reservar(ana.UsuarioId, instructoraYoga.EntrenadorId, huecos.Last()));
citas.Anular(cita.CitaId);
Ok("Cita anulada.");

Titulo("9. Rutinas");
var rutina = rutinas.Crear(ana.UsuarioId, "Pierna lunes");
var sentadilla = ejercicios.BuscarPorGrupo("Piernas").First();
var detalle = rutinas.AnadirEjercicio(rutina.RutinaId, sentadilla.EjercicioId, 1, 4, 12);
Ok($"Rutina '{rutina.NombreRutina}' con {sentadilla.NombreEjercicio} {detalle.Series}x{detalle.Repeticiones}");
Probar("Configurar 0 series", () => rutinas.ConfigurarEjercicio(detalle.DetalleRutinaId, 0, 12));
rutinas.ConfigurarEjercicio(detalle.DetalleRutinaId, 5, 10);
Ok($"Modificación correcta: {sentadilla.NombreEjercicio} pasa de 4x12 a {detalle.Series}x{detalle.Repeticiones}");
rutinas.Eliminar(rutina.RutinaId);
Ok("Rutina eliminada (sus ejercicios configurados se borran en cascada).");


//LECTURA XML
Titulo("10. Lectura de datos desde XML");

string rutaXml = Path.Combine(AppContext.BaseDirectory, "gentefit-datos.xml");

void LeerActividadesXml(string descripcion, string ruta)
{
    var resultado = GenteFit.Xml.LectorXml.leerActividades(ruta);

    if (resultado.IsError)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  {descripcion}: {resultado.ErrorValue}");
        Console.ResetColor();
        return;
    }

    var lectura = resultado.ResultValue;
    Console.WriteLine($"  {descripcion}");
    foreach (var a in lectura.Elementos)
    {
        string desc = a.Descripcion ?? "";
        if (desc.Length > 50) desc = desc[..50] + "...";
        Console.WriteLine($"    - {a.NombreActividad} ({a.Intensidad}) {desc}");
    }
    foreach (var aviso in lectura.Avisos)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {descripcion}: {aviso}");
        Console.ResetColor();
    }
}

// a) Caso normal: acentos, apóstrofes y el símbolo & incluidos.
LeerActividadesXml("a) Fichero correcto", rutaXml);

// b) Fichero que no existe.
LeerActividadesXml("b) Fichero inexistente", Path.Combine(AppContext.BaseDirectory, "no-existe.xml"));

// c) Fichero mal formado (falta cerrar etiquetas).
string xmlRoto = Path.GetTempFileName();
File.WriteAllText(xmlRoto, "<gentefit><actividades><actividad><nombre>Yoga</nombre></gentefit>");
LeerActividadesXml("c) XML mal formado", xmlRoto); File.Delete(xmlRoto);

// d) Una actividad sin <nombre>: se ignora y se lee el resto.
string xmlIncompleto = Path.GetTempFileName();
File.WriteAllText(xmlIncompleto,
    "<gentefit><actividades>" +
    "<actividad><especialidad>YOGA</especialidad><intensidad>BAJA</intensidad></actividad>" +
    "<actividad><nombre>Pilates</nombre><especialidad>PILATES</especialidad><intensidad>MEDIA</intensidad></actividad>" +
    "</actividades></gentefit>");
LeerActividadesXml("d) Actividad sin nombre", xmlIncompleto);
File.Delete(xmlIncompleto);

//IMPORTAR
Titulo("11. Importar clientes desde XML a SQL Server");

var importacion = GenteFit.Xml.LectorXml.leerUsuarios(rutaXml);
if (importacion.IsError)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Importar clientes: {importacion.ErrorValue}");
    Console.ResetColor();
}
else
{
    foreach (var cliente in importacion.ResultValue.Elementos)
    {
        bool estabaActivo = cliente.Activo;
        try
        {
            var guardado = usuarios.Crear(cliente, InicializadorBD.ContrasenaPrueba);
            if (!estabaActivo)
                usuarios.DarDeBaja(guardado.UsuarioId);
            Ok($"Importado: {guardado.Nombre} {guardado.Apellido1} ({guardado.Email}){(estabaActivo ? "" : " - dado de baja")}");
        }
        catch (ValidacionException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  No importado ({cliente.Email}): {ex.Message}");
            Console.ResetColor();
        }
    }

    foreach (var aviso in importacion.ResultValue.Avisos)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Importar clientes: {aviso}");
        Console.ResetColor();
    }
}

//EXPORTAR
Titulo("12. Exportar clientes de SQL Server a XML");

string rutaExportacion = Path.Combine(AppContext.BaseDirectory, "clientes-exportados.xml");
var clientesBD = usuarios.ObtenerClientes();

var exportacion = GenteFit.Xml.EscritorXml.escribirUsuarios(rutaExportacion, clientesBD);
if (exportacion.IsError)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Exportar clientes: {exportacion.ErrorValue}");
    Console.ResetColor();
}
else
{
    Ok($"Exportados {exportacion.ResultValue} clientes a: {rutaExportacion}");

    // Comprobación: se vuelve a leer el fichero generado con el mismo lector.
    var comprobacion = GenteFit.Xml.LectorXml.leerUsuarios(rutaExportacion);
    if (comprobacion.IsOk)
        Ok($"Comprobación: al volver a leerlo salen {comprobacion.ResultValue.Elementos.Length} clientes.");
}

// Error esperado: la carpeta no existe.
var exportacionMala = GenteFit.Xml.EscritorXml.escribirUsuarios(
    Path.Combine(AppContext.BaseDirectory, "carpeta-inexistente", "clientes.xml"), clientesBD);
if (exportacionMala.IsError)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Exportar a carpeta inexistente: {exportacionMala.ErrorValue}");
    Console.ResetColor();
}

Titulo("Fin de la demo");

// ---------- Funciones auxiliares ----------

List<Usuario> ObtenerClientesDemo(int cantidad)
{
    var lista = new List<Usuario>();
    for (int i = 1; i <= cantidad; i++)
    {
        string email = $"demo{i}@correo.com";
        var existente = usuarios.Buscar(email).FirstOrDefault();
        lista.Add(existente ?? usuarios.Crear(new Usuario
        {
            Nombre = $"Demo{i}",
            Apellido1 = "Prueba",
            Dni = $"DEMO{i:D4}",
            Email = email,
            Telefono = 690000000 + i,
            Rol = Rol.Cliente
        }, InicializadorBD.ContrasenaPrueba));
    }
    return lista;
}

void MostrarEstado(Guid sesionId)
{
    Console.WriteLine($"  Con plaza: {reservas.ListarConfirmados(sesionId).Count} · Plazas libres: {sesiones.PlazasDisponibles(sesionId)}");
    int posicion = 1;
    foreach (var r in reservas.ListarListaEspera(sesionId))
        Console.WriteLine($"  Lista de espera {posicion++}: {r.Usuario.Nombre}");
}

void Probar(string descripcion, Action accion)
{
    try
    {
        accion();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {descripcion}: no ha dado error (¡revisar!)");
    }
    catch (ValidacionException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  {descripcion}: {ex.Message}");
    }
    Console.ResetColor();
}

void Titulo(string texto)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n=== {texto} ===");
    Console.ResetColor();
}

void Ok(string texto)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  {texto}");
    Console.ResetColor();
}