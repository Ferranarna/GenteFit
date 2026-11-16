using System;
using GenteFit.Datos.Seguridad;
using GenteFit.Modelo.Entidades;
using GenteFit.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenteFit.Datos;

public static class InicializadorBD
{
    // Contraseña de todos los usuarios de prueba
    public const string ContrasenaPrueba = "GenteFit123";

    public static void Inicializar(GenteFitContext db)
    {
        db.Database.Migrate();

        if (!db.Salas.Any())
            CargarDatosPrueba(db);
    }

    public static void CargarDatosPrueba(GenteFitContext db)
    {
        var ahora = DateTime.Now;
        var hoy = DateTime.Today;
        var azar = new Random(2026); // semilla fija: cada vez que se recrea la BD salen los mismos datos
        string hash = HashContrasena.Generar(ContrasenaPrueba);

        // ---------------------------------------------------------------- Salas
        var salaGrande = new Sala { NombreSala = "Sala Grande", Ubicacion = Ubicacion.Planta1 };
        var salaCiclo = new Sala { NombreSala = "Sala Ciclo", Ubicacion = Ubicacion.Planta2 };
        var salaZen = new Sala { NombreSala = "Sala Zen", Ubicacion = Ubicacion.Planta2 };
        db.Salas.AddRange(salaGrande, salaCiclo, salaZen);

        // ---------------------------------------------------------------- Actividades
        // Aforos pequeños a propósito: así se puede probar la lista de espera con pocos clientes.
        var yoga = new Actividad { NombreActividad = "Yoga", Descripcion = "Posturas, respiración y estiramientos para ganar flexibilidad y equilibrio y relajar la mente.", Intensidad = Intensidad.Baja, Especialidad = Especialidad.Yoga, LimiteUsuarios = 10, UrlImagen = "~/img/actividades/yoga.jpg" };
        var pilates = new Actividad { NombreActividad = "Pilates", Descripcion = "Ejercicios controlados que fortalecen el abdomen y la espalda y mejoran la postura.", Intensidad = Intensidad.Baja, Especialidad = Especialidad.Pilates, LimiteUsuarios = 8, UrlImagen = "~/img/actividades/pilates.jpg" };
        var core = new Actividad { NombreActividad = "Core", Descripcion = "Entrenamiento del abdomen, la zona lumbar y el centro del cuerpo para ganar estabilidad y fuerza.", Intensidad = Intensidad.Media, Especialidad = Especialidad.Core, LimiteUsuarios = 10, UrlImagen = "~/img/actividades/core.jpg" };
        var zumba = new Actividad { NombreActividad = "Zumba", Descripcion = "Baile con ritmos latinos y mucho cardio para quemar calorías divirtiéndote.", Intensidad = Intensidad.Media, Especialidad = Especialidad.Zumba, LimiteUsuarios = 12, UrlImagen = "~/img/actividades/zumba.jpg" };
        var spinning = new Actividad { NombreActividad = "Spinning", Descripcion = "Ciclismo indoor con música, cambios de ritmo y resistencia para mejorar la resistencia cardiovascular.", Intensidad = Intensidad.Alta, Especialidad = Especialidad.Spinning, LimiteUsuarios = 8, UrlImagen = "~/img/actividades/spinning.jpg" };
        var bodypump = new Actividad { NombreActividad = "Body Pump", Descripcion = "Trabajo de fuerza con barra y discos, muchas repeticiones y poco peso para tonificar todo el cuerpo.", Intensidad = Intensidad.Alta, Especialidad = Especialidad.Bodypump, LimiteUsuarios = 5, UrlImagen = "~/img/actividades/bodypump.jpg" };
        var bodycombat = new Actividad { NombreActividad = "Body Combat", Descripcion = "Cardio inspirado en artes marciales: golpes y patadas al ritmo de la música, sin contacto.", Intensidad = Intensidad.Alta, Especialidad = Especialidad.BodyCombat, LimiteUsuarios = 10, UrlImagen = "~/img/actividades/bodycombat.jpg" };
        db.Actividades.AddRange(yoga, pilates, core, zumba, spinning, bodypump, bodycombat);

        // ---------------------------------------------------------------- Ejercicios
        var sentadilla = new Ejercicio { NombreEjercicio = "Sentadilla", GrupoMuscular = "Piernas", Descripcion = "Flexiona rodillas y cadera como para sentarte, con la espalda recta, y sube empujando con los talones.", UrlImagen = "~/img/ejercicios/sentadilla.jpg" };
        var zancada = new Ejercicio { NombreEjercicio = "Zancada", GrupoMuscular = "Piernas", Descripcion = "Da un paso largo adelante y baja hasta que ambas rodillas formen unos 90 grados. Vuelve y cambia de pierna.", UrlImagen = "~/img/ejercicios/zancada.jpg" };
        var puenteGluteo = new Ejercicio { NombreEjercicio = "Puente de glúteo", GrupoMuscular = "Glúteos", Descripcion = "Tumbado boca arriba con las rodillas dobladas, eleva la cadera apretando los glúteos y baja con control.", UrlImagen = "~/img/ejercicios/puente-gluteo.jpg" };
        var pesoMuerto = new Ejercicio { NombreEjercicio = "Peso muerto", GrupoMuscular = "Glúteos", Descripcion = "Con la barra pegada al cuerpo, lleva la cadera hacia atrás con la espalda recta y vuelve a la posición erguida.", UrlImagen = "~/img/ejercicios/peso-muerto.jpg" };
        var pressBanca = new Ejercicio { NombreEjercicio = "Press banca", GrupoMuscular = "Pecho", Descripcion = "Tumbado en el banco, baja la barra hasta el pecho y empújala hacia arriba hasta extender los brazos.", UrlImagen = "~/img/ejercicios/press-banca.jpg" };
        var flexiones = new Ejercicio { NombreEjercicio = "Flexiones", GrupoMuscular = "Pecho", Descripcion = "Con las manos bajo los hombros y el cuerpo en línea recta, baja el pecho al suelo y empuja para subir.", UrlImagen = "~/img/ejercicios/flexiones.jpg" };
        var remoBarra = new Ejercicio { NombreEjercicio = "Remo con barra", GrupoMuscular = "Espalda", Descripcion = "Inclinado hacia delante con la espalda recta, tira de la barra hacia el abdomen juntando los omóplatos.", UrlImagen = "~/img/ejercicios/remo-barra.jpg" };
        var dominadas = new Ejercicio { NombreEjercicio = "Dominadas", GrupoMuscular = "Espalda", Descripcion = "Colgado de la barra, tira de tu cuerpo hacia arriba hasta pasar la barbilla y baja con control.", UrlImagen = "~/img/ejercicios/dominadas.jpg" };
        var pressMilitar = new Ejercicio { NombreEjercicio = "Press militar", GrupoMuscular = "Hombros", Descripcion = "De pie, empuja la barra desde los hombros hasta estirar los brazos por encima de la cabeza.", UrlImagen = "~/img/ejercicios/press-militar.jpg" };
        var elevacionesLaterales = new Ejercicio { NombreEjercicio = "Elevaciones laterales", GrupoMuscular = "Hombros", Descripcion = "De pie con una mancuerna en cada mano, eleva los brazos hacia los lados hasta la altura de los hombros.", UrlImagen = "~/img/ejercicios/elevaciones-laterales.jpg" };
        var curlBiceps = new Ejercicio { NombreEjercicio = "Curl de bíceps", GrupoMuscular = "Brazos", Descripcion = "Con una mancuerna en cada mano y los codos pegados al cuerpo, flexiona los brazos y baja despacio.", UrlImagen = "~/img/ejercicios/curl-biceps.jpg" };
        var fondosBanco = new Ejercicio { NombreEjercicio = "Fondos en banco", GrupoMuscular = "Brazos", Descripcion = "Apoyado con las manos en un banco, flexiona los codos para bajar el cuerpo y empuja para subir.", UrlImagen = "~/img/ejercicios/fondos-banco.jpg" };
        var plancha = new Ejercicio { NombreEjercicio = "Plancha", GrupoMuscular = "Core", Descripcion = "Apoyado en antebrazos y puntas de los pies, mantén el cuerpo recto de la cabeza a los talones.", UrlImagen = "~/img/ejercicios/plancha.jpg" };
        var crunch = new Ejercicio { NombreEjercicio = "Crunch abdominal", GrupoMuscular = "Core", Descripcion = "Tumbado boca arriba con las rodillas dobladas, eleva los hombros del suelo contrayendo el abdomen.", UrlImagen = "~/img/ejercicios/crunch-abdominal.jpg" };
        db.Ejercicios.AddRange(sentadilla, zancada, puenteGluteo, pesoMuerto, pressBanca, flexiones, remoBarra, dominadas,
            pressMilitar, elevacionesLaterales, curlBiceps, fondosBanco, plancha, crunch);

        // ---------------------------------------------------------------- Personal del centro
        db.Usuarios.AddRange(
            NuevoUsuario("Admin", "GenteFit", "00000000A", "admin@gentefit.com", Rol.Administrador, 611000000, hash),
            NuevoUsuario("Rosa", "Martín", "00000001B", "recepcion@gentefit.com", Rol.Recepcionista, 611000001, hash),
            NuevoUsuario("Pablo", "Navarro", "00000003D", "recepcion2@gentefit.com", Rol.Recepcionista, 611000003, hash),
            NuevoUsuario("Jordi", "Serra", "00000002C", "encargado@gentefit.com", Rol.Encargado, 611000002, hash),
            NuevoUsuario("Carmen", "Vega", "00000004E", "encargada2@gentefit.com", Rol.Encargado, 611000004, hash));

        // ---------------------------------------------------------------- Entrenadores
        // Cada uno es un usuario con rol Entrenador (inicia sesión con su email).
        Entrenador NuevoEntrenador(string nombre, string apellido, string dni, string email, int telefono, Especialidad especialidad, bool activo = true) =>
            new Entrenador { Usuario = NuevoUsuario(nombre, apellido, dni, email, Rol.Entrenador, telefono, hash, activo), Especialidad = especialidad };

        var laura = NuevoEntrenador("Laura", "Gómez", "44444441D", "laura@gentefit.com", 600111222, Especialidad.Yoga);
        var elena = NuevoEntrenador("Elena", "Mora", "44444446J", "elena@gentefit.com", 600666777, Especialidad.Pilates);
        var marc = NuevoEntrenador("Marc", "Puig", "44444442E", "marc@gentefit.com", 600222333, Especialidad.Spinning);
        var sara = NuevoEntrenador("Sara", "Ruiz", "44444443F", "sara@gentefit.com", 600333444, Especialidad.Zumba);
        var pau = NuevoEntrenador("Pau", "Vidal", "44444444G", "pau@gentefit.com", 600444555, Especialidad.Bodypump);
        var ines = NuevoEntrenador("Inés", "Ortega", "44444447K", "ines@gentefit.com", 600777888, Especialidad.BodyCombat);
        var nuria = NuevoEntrenador("Nuria", "Campos", "44444448L", "nuria@gentefit.com", 600888999, Especialidad.Core);
        var david = NuevoEntrenador("David", "López", "44444445H", "david@gentefit.com", 600555666, Especialidad.EntrenadorPersonal);
        var raul = NuevoEntrenador("Raúl", "Nieto", "44444449M", "raul@gentefit.com", 600999000, Especialidad.EntrenadorPersonal);
        // Entrenador de baja, para probar los filtros y los informes
        var tomas = NuevoEntrenador("Tomás", "Gil", "44444450N", "tomas@gentefit.com", 600000111, Especialidad.Zumba, activo: false);
        db.Entrenadores.AddRange(laura, elena, marc, sara, pau, ines, nuria, david, raul, tomas);

        // ---------------------------------------------------------------- Clientes (12, dos de baja)
        var ana = NuevoUsuario("Ana", "García", "11111111A", "ana@correo.com", Rol.Cliente, 622000001, hash);
        var luis = NuevoUsuario("Luis", "Pérez", "22222222B", "luis@correo.com", Rol.Cliente, 622000002, hash);
        var marta = NuevoUsuario("Marta", "Soler", "33333333C", "marta@correo.com", Rol.Cliente, 622000003, hash);
        var pedro = NuevoUsuario("Pedro", "Molina", "55555501A", "pedro@correo.com", Rol.Cliente, 622000004, hash);
        var lucia = NuevoUsuario("Lucía", "Ferrer", "55555502B", "lucia@correo.com", Rol.Cliente, 622000005, hash);
        var sergio = NuevoUsuario("Sergio", "Ibáñez", "55555503C", "sergio@correo.com", Rol.Cliente, 622000006, hash);
        var paula = NuevoUsuario("Paula", "Roca", "55555504D", "paula@correo.com", Rol.Cliente, 622000007, hash);
        var alvaro = NuevoUsuario("Álvaro", "Prat", "55555505E", "alvaro@correo.com", Rol.Cliente, 622000008, hash);
        var irene = NuevoUsuario("Irene", "Costa", "55555506F", "irene@correo.com", Rol.Cliente, 622000009, hash);
        var raquel = NuevoUsuario("Raquel", "Bosch", "55555507G", "raquel@correo.com", Rol.Cliente, 622000010, hash);
        var daniel = NuevoUsuario("Daniel", "Font", "55555508H", "daniel@correo.com", Rol.Cliente, 622000011, hash, activo: false);
        var clara = NuevoUsuario("Clara", "Mas", "55555509J", "clara@correo.com", Rol.Cliente, 622000012, hash, activo: false);
        db.Usuarios.AddRange(ana, luis, marta, pedro, lucia, sergio, paula, alvaro, irene, raquel, daniel, clara);

        var todosLosClientes = new List<Usuario> { ana, luis, marta, pedro, lucia, sergio, paula, alvaro, irene, raquel, daniel, clara };
        var clientesActivos = todosLosClientes.Where(c => c.Activo).ToList();

        // ---------------------------------------------------------------- Sesiones
        // De hace 3 semanas a dentro de 2: las pasadas alimentan informes y estadísticas del mes.
        // Todas las actividades se dan todos los días; no se pisan ni salas ni entrenadores.
        var sesiones = new List<Sesion>();

        Sesion Programar(Actividad actividad, Sala sala, Entrenador entrenador, DateTime dia, int hora, int minuto, int duracion) =>
            new Sesion
            {
                Actividad = actividad,
                Sala = sala,
                Entrenador = entrenador,
                Fecha = dia.AddHours(hora).AddMinutes(minuto),
                Duracion = duracion
            };

        for (int dia = -21; dia <= 14; dia++)
        {
            var fecha = hoy.AddDays(dia);
            sesiones.Add(Programar(yoga, salaZen, laura, fecha, 9, 0, 60));
            sesiones.Add(Programar(pilates, salaZen, elena, fecha, 11, 0, 60));
            sesiones.Add(Programar(core, salaGrande, nuria, fecha, 12, 0, 45));
            sesiones.Add(Programar(spinning, salaCiclo, marc, fecha, 18, 0, 45));
            sesiones.Add(Programar(bodycombat, salaGrande, ines, fecha, 18, 0, 55));
            sesiones.Add(Programar(zumba, salaGrande, sara, fecha, 19, 0, 60));
            sesiones.Add(Programar(bodypump, salaGrande, pau, fecha, 20, 0, 60));
        }
        db.Sesiones.AddRange(sesiones);

        // La sesión de una actividad en un día (0 = hoy, 1 = mañana, -1 = ayer…)
        Sesion SesionDe(Actividad actividad, int dia) =>
            sesiones.First(s => s.Actividad == actividad && s.Fecha.Date == hoy.AddDays(dia));

        // Algunas sesiones canceladas (pasadas y futuras)
        foreach (var (actividad, dia) in new[] { (yoga, -5), (spinning, -2), (zumba, -9), (pilates, 3), (bodycombat, 6) })
            SesionDe(actividad, dia).Estado = EstadoSesion.Cancelada;

        // ---------------------------------------------------------------- Reservas
        var reservas = new List<Reserva>();
        var yaReservadas = new HashSet<(Usuario, Sesion)>();

        void Reservar(Usuario cliente, Sesion sesion, EstadoReserva estado, DateTime? cuando = null)
        {
            if (!yaReservadas.Add((cliente, sesion))) return;

            var fecha = cuando ?? sesion.Fecha.AddDays(-azar.Next(1, 4)).AddMinutes(azar.Next(0, 600));
            if (fecha > ahora) fecha = ahora.AddMinutes(-azar.Next(30, 3000));

            reservas.Add(new Reserva { Usuario = cliente, Sesion = sesion, Estado = estado, FechaReserva = fecha });
        }

        // a) Lista de espera: Body Pump de mañana (5 plazas): 5 confirmadas y 3 esperando, por orden de llegada
        var bodypumpManana = SesionDe(bodypump, 1);
        var confirmadasBP = new[] { ana, luis, pedro, lucia, sergio };
        for (int i = 0; i < confirmadasBP.Length; i++)
            Reservar(confirmadasBP[i], bodypumpManana, EstadoReserva.Confirmada, ahora.AddHours(-40).AddMinutes(i * 10));
        var enEsperaBP = new[] { marta, paula, alvaro };
        for (int i = 0; i < enEsperaBP.Length; i++)
            Reservar(enEsperaBP[i], bodypumpManana, EstadoReserva.ListaEspera, ahora.AddHours(-20).AddMinutes(i * 10));

        // b) Spinning de pasado mañana (8 plazas) completo y sin lista de espera
        var spinningPasado = SesionDe(spinning, 2);
        foreach (var cliente in clientesActivos.Take(8))
            Reservar(cliente, spinningPasado, EstadoReserva.Confirmada);

        // c) Reservas concretas de Ana, Luis y Marta, para tener un caso de cada cosa
        Reservar(ana, SesionDe(yoga, 1), EstadoReserva.Confirmada);
        Reservar(ana, SesionDe(zumba, 2), EstadoReserva.Confirmada);
        Reservar(ana, SesionDe(core, 3), EstadoReserva.Confirmada);
        Reservar(ana, SesionDe(pilates, 2), EstadoReserva.Cancelada);      // reserva anulada
        Reservar(ana, SesionDe(yoga, -3), EstadoReserva.Confirmada);
        Reservar(ana, SesionDe(spinning, -1), EstadoReserva.Confirmada);
        Reservar(luis, SesionDe(spinning, 1), EstadoReserva.Confirmada);
        Reservar(luis, SesionDe(yoga, -2), EstadoReserva.Cancelada);
        Reservar(luis, SesionDe(bodypump, -2), EstadoReserva.Confirmada);
        Reservar(marta, SesionDe(zumba, 2), EstadoReserva.Confirmada);
        Reservar(marta, SesionDe(zumba, -2), EstadoReserva.Confirmada);

        // d) El resto, al azar: las pasadas y las de la próxima semana. Más allá, sin reservas.
        foreach (var sesion in sesiones)
        {
            if (sesion.Fecha.Date > hoy.AddDays(7)) continue;

            bool pasada = sesion.Fecha <= ahora;
            var candidatos = (pasada ? todosLosClientes : clientesActivos).OrderBy(_ => azar.Next()).ToList();

            // En una sesión cancelada, solo quedan reservas anuladas
            if (sesion.Estado == EstadoSesion.Cancelada)
            {
                foreach (var cliente in candidatos.Take(azar.Next(1, 4)))
                    Reservar(cliente, sesion, EstadoReserva.Cancelada);
                continue;
            }

            int limite = sesion.Actividad.LimiteUsuarios;
            int yaConfirmadas = reservas.Count(r => r.Sesion == sesion && r.Estado == EstadoReserva.Confirmada);
            int quieren = pasada ? azar.Next(2, limite + 1) : azar.Next(0, limite);

            foreach (var cliente in candidatos.Take(Math.Max(0, quieren - yaConfirmadas)))
            {
                var estado = pasada && azar.NextDouble() < 0.12 ? EstadoReserva.Cancelada : EstadoReserva.Confirmada;
                Reservar(cliente, sesion, estado);
            }
        }
        db.Reservas.AddRange(reservas);

        // ---------------------------------------------------------------- Citas (siempre de 30 minutos)
        var citas = new List<Cita>();
        var citasDelCliente = new HashSet<(Usuario, DateTime)>();
        var citasDelEntrenador = new HashSet<(Entrenador, DateTime)>();

        void PedirCita(Usuario cliente, Entrenador entrenador, DateTime fecha, EstadoCita estado = EstadoCita.Programada)
        {
            if (!citasDelCliente.Add((cliente, fecha)) || !citasDelEntrenador.Add((entrenador, fecha))) return;
            citas.Add(new Cita { Usuario = cliente, Entrenador = entrenador, FechaActividad = fecha, Duracion = 30, Estado = estado });
        }

        // a) Concretas: próximas, una anulada y una pasada
        PedirCita(ana, david, hoy.AddDays(1).AddHours(17));
        PedirCita(ana, raul, hoy.AddDays(4).AddHours(10));
        PedirCita(ana, david, hoy.AddDays(3).AddHours(18), EstadoCita.Cancelada);
        PedirCita(luis, raul, hoy.AddDays(2).AddHours(11));
        PedirCita(marta, david, hoy.AddDays(-2).AddHours(10));
        PedirCita(pedro, david, hoy.AddDays(1).AddHours(9).AddMinutes(30));

        // b) El resto, al azar entre los dos entrenadores personales
        var horas = new (int Hora, int Minuto)[] { (9, 0), (9, 30), (10, 0), (10, 30), (11, 0), (12, 0), (16, 0), (16, 30), (17, 0), (17, 30), (18, 0) };
        foreach (var entrenador in new[] { david, raul })
        {
            for (int dia = -21; dia <= 14; dia++)
            {
                foreach (var (hora, minuto) in horas.OrderBy(_ => azar.Next()).Take(azar.Next(0, 4)))
                {
                    var fecha = hoy.AddDays(dia).AddHours(hora).AddMinutes(minuto);
                    var cliente = (fecha <= ahora ? todosLosClientes : clientesActivos)[azar.Next(fecha <= ahora ? todosLosClientes.Count : clientesActivos.Count)];
                    PedirCita(cliente, entrenador, fecha, azar.NextDouble() < 0.1 ? EstadoCita.Cancelada : EstadoCita.Programada);
                }
            }
        }
        db.Citas.AddRange(citas);

        // ---------------------------------------------------------------- Rutinas
        Rutina NuevaRutina(string nombre, Usuario cliente, params (Ejercicio Ejercicio, int Series, int Repeticiones)[] filas)
        {
            var rutina = new Rutina { NombreRutina = nombre, Usuario = cliente };
            for (int i = 0; i < filas.Length; i++)
                rutina.Detalles.Add(new DetalleRutina { Orden = i + 1, Series = filas[i].Series, Repeticiones = filas[i].Repeticiones, Ejercicio = filas[i].Ejercicio });
            return rutina;
        }

        db.Rutinas.AddRange(
            NuevaRutina("Pierna y core", ana, (sentadilla, 4, 12), (zancada, 3, 10), (plancha, 3, 30)),
            NuevaRutina("Torso", ana, (pressBanca, 4, 8), (remoBarra, 4, 10), (pressMilitar, 3, 10), (curlBiceps, 3, 12)),
            NuevaRutina("Fuerza", luis, (pesoMuerto, 4, 6), (sentadilla, 4, 8), (dominadas, 3, 6)),
            NuevaRutina("Cardio y core", marta, (flexiones, 3, 15), (crunch, 3, 20), (plancha, 3, 40)),
            NuevaRutina("Brazos", pedro, (curlBiceps, 4, 12), (fondosBanco, 3, 12)),
            NuevaRutina("Glúteos", lucia, (puenteGluteo, 4, 15), (pesoMuerto, 3, 10), (zancada, 3, 12)),
            NuevaRutina("Hombros", sergio, (pressMilitar, 4, 10), (elevacionesLaterales, 3, 15)));

        db.SaveChanges();
    }

    private static Usuario NuevoUsuario(string nombre, string apellido, string dni, string email, Rol rol, int telefono, string hash, bool activo = true) =>
        new Usuario
        {
            Nombre = nombre,
            Apellido1 = apellido,
            Dni = dni,
            Email = email,
            Rol = rol,
            Telefono = telefono,
            Contrasena = hash,
            Activo = activo
        };
}
