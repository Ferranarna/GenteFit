# GenteFit

Aplicación web de gestión de un centro deportivo (clientes, actividades, salas, sesiones, reservas con lista de espera, citas con entrenador personal y rutinas), desarrollada en .NET para la asignatura **FP.452 – Técnicas de persistencia de datos con .NET y apps ERP** (DAM).

Equipo: **BugBusters**

## Estado por productos

| Producto | Contenido | Estado |
|---|---|---|
| 1 | Diseño UML y pantallas | Entregado |
| 2 | Capa de acceso a datos (ORM + SQL Server) y capa XML | Entregado |
| 3 | Aplicación web ASP.NET Core MVC | Entregado |
| 4 | Intercambio de datos con Odoo (Python + XML) | Pendiente |
| 5 | Implantación del ERP | Pendiente |

## Funcionalidades

La aplicación tiene cinco perfiles y cada uno solo ve sus pantallas:

- **Cliente:** consulta actividades, ejercicios y sesiones; reserva plaza o se apunta a la lista de espera; gestiona sus reservas, citas y rutinas.
- **Recepcionista:** da de alta, modifica, da de baja y reactiva clientes; consulta actividades y sesiones.
- **Encargado:** gestiona actividades, salas, ejercicios, entrenadores y sesiones; consulta las reservas y la lista de espera; saca informes.
- **Entrenador:** consulta sus sesiones y los ejercicios. El **entrenador personal**, además, gestiona sus citas y crea rutinas a sus clientes.
- **Administrador:** acceso total, incluida la gestión de usuarios y roles y la pantalla de Odoo (de momento solo diseño).

## Arquitectura

Arquitectura **por capas**, una solución de Visual Studio (.NET 8) con un proyecto por capa. Cada capa solo conoce a las que tiene "debajo", nunca al revés.

```
Presentación  (GenteFit.Web, ASP.NET Core MVC)  <- Producto 3
      |
 Datos / XML  (GenteFit.Datos, GenteFit.Xml)    <- Producto 2
      |
   Modelo     (GenteFit.Modelo)                 <- clases del diagrama de clases (Producto 1)
```

Una petición viaja así: **navegador → controlador (`[Authorize(Roles)]`) → repositorio (valida y aplica las reglas) → `GenteFitContext` (EF Core) → SQL Server**.

### Estructura de carpetas

```
GenteFit/
├── GenteFit.sln
├── README.md
├── src/
│   ├── GenteFit.Modelo/   (C#)  Entidades del dominio y enumerados
│   ├── GenteFit.Datos/    (C#)  EF Core, migraciones, repositorios y reglas de negocio
│   ├── GenteFit.Xml/      (F#)  Lectura y escritura de ficheros XML
│   ├── GenteFit.Web/      (C#)  Aplicación web: controladores, vistas Razor, CSS
│   ├── GenteFit.Demo/     (C#)  Aplicación de consola del Producto 2
│   └── GenteFit.Tests/    (C#)  Pruebas automáticas (xUnit)
├── instalador/                  Script de Inno Setup y GenteFit.bat
├── publicar/                    Web publicada (generada con dotnet publish; no se sube a Git)
└── salida/                      Instalador setup.exe (generado; no se sube a Git)
```

### Responsabilidad de cada proyecto

- **Modelo:** clases simples que representan los datos. No saben nada de SQL, XML ni pantallas.
- **Datos:** guarda y recupera el modelo en SQL Server. Los repositorios validan y aplican las reglas (plazas, lista de espera, citas, etc.).
- **Xml:** lee ficheros XML y los convierte en objetos del modelo, y guarda objetos del modelo en XML. Es independiente de la base de datos.
- **Web:** muestra la información y recoge las acciones del usuario. No escribe SQL ni reglas de negocio: las pide a los repositorios.
- **Demo:** prueba la capa de datos y la capa XML sin interfaz gráfica.
- **Tests:** 148 pruebas de la capa de datos con SQLite en memoria.

### Lenguajes y tecnologías

- **C# (.NET 8):** modelo, datos, web y pruebas. ASP.NET Core MVC con vistas Razor, Entity Framework Core 8 y SQL Server.
- **F#:** capa XML.
- **Bootstrap 5 + CSS propio** (clases `gf-`), **xUnit** para las pruebas e **Inno Setup** para el instalador.

### Requisitos (solo la primera vez)

- **SQL Server 2025 Developer:** para guardar y gestionar la base de datos.
- **SSMS (SQL Server Management Studio):** para ver y manejar la base de datos.
- **.NET 8 SDK:** kit para construir programas en .NET.
- **Herramienta de EF Core (`dotnet-ef` 8.0.11):** añade el comando `dotnet ef`, con el que se crea y actualiza la estructura de la base de datos local (migraciones) y se puede borrar.

Para empezar de cero con los datos de ejemplo:

```bash
dotnet ef database drop --project src/GenteFit.Datos
dotnet run --project src/GenteFit.Web
```

## Cómo ejecutarlo

Requisitos: SDK de .NET 8 y SQL Server en marcha (instalación normal o Express).

```bash
git clone https://github.com/Ferranarna/GenteFit
cd GenteFit
dotnet build
dotnet run --project src/GenteFit.Web
```

Abre `http://localhost:5173`. Al arrancar se crea la base de datos y se aplican las migraciones; si está vacía, se llena con datos de ejemplo. La cadena de conexión está en `src/GenteFit.Web/appsettings.json` (por defecto `Server=localhost`; con Express, `localhost\SQLEXPRESS`).

**Usuarios de ejemplo** (contraseña de todos: `GenteFit123`):

| Rol | Email |
|---|---|
| Administrador | `administrador_lidia@gentefit.com` |
| Recepcionista | `recepcionista_rosa@gentefit.com` |
| Encargado | `encargado_jordi@gentefit.com` |
| Entrenador personal | `entrenador_david@gentefit.com` |
| Cliente | `cliente_ana@correo.com` |

Para ejecutar las pruebas: `dotnet test`.

## Instalador (Windows)

1. Publicar la web: `dotnet publish src/GenteFit.Web -c Release -r win-x64 --self-contained true -o publicar`
2. Abrir `instalador/GenteFit.iss` con **Inno Setup** y compilar: crea `salida/setup.exe`.

El instalador pregunta el servidor de SQL Server, copia la aplicación en Archivos de programa y crea accesos directos a `GenteFit.bat`, que arranca el servidor y abre el navegador.

## Base de datos (SQL Server + EF Core)

Cada miembro tiene su propia copia local de la base de datos; lo que se comparte por Git es la estructura (migraciones en `src/GenteFit.Datos/Migrations`: `Inicial`, `EntrenadorUsuario` y `RutinaCreador`).


## Uso de herramientas de IA

- **ChatGPT:** Generación de las imágenes de actividades y ejercicios (`wwwroot/img`).
- **Claude:** Claude se ha utilizado como apoyo en consultas, revisión y redacción.