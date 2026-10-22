# GenteFit

Aplicación de gestión de un centro deportivo (clientes, actividades, horarios, reservas y lista de espera), desarrollada en .NET para la asignatura **FP.452 – Técnicas de persistencia de datos con .NET y apps ERP** (DAM).

Equipo: **BugBusters** 

## Estado por productos

| Producto | Contenido | Estado |
|---|---|---|
| 1 | Diseño UML y pantallas | Entregado |
| 2 | Capa de acceso a datos (ORM + SQL Server, lectura XML) | En curso |
| 3 | Aplicación Windows Forms | Pendiente |
| 4 | Intercambio de datos con Odoo (Python + XML) | Pendiente |
| 5 | Implantación del ERP | Pendiente |

## Arquitectura

Arquitectura **por capas**, una solución de Visual Studio (.NET 8) con un proyecto por capa. Cada capa solo conoce a las que tiene "debajo", nunca al revés.

```
Presentación  (GenteFit.UI)                   <- Producto 3
      |
   Negocio    (GenteFit.Negocio)              <- Producto 3
      |
 Datos / XML  (GenteFit.Datos, GenteFit.Xml)  <- Producto 2
      |
   Modelo     (GenteFit.Modelo)               <- clases del diagrama de clases (Producto 1)
```

### Estructura de carpetas

```
GenteFit/
├── GenteFit.sln
├── README.md
├── .gitignore
├── src/
│   ├── GenteFit.Modelo/      (C#)  Entidades del dominio: Cliente, Actividad, Reserva...
│   ├── GenteFit.Datos/       (C#)  Acceso a SQL Server mediante el ORM elegido
│   ├── GenteFit.Xml/         (F#)  Lectura y escritura de la fuente de datos XML
│   ├── GenteFit.Negocio/     (C#)  Reglas: 16 plazas, lista de espera... (Producto 3)
│   └── GenteFit.UI/          (C#)  Windows Forms (Producto 3)
├── tests/                          Pruebas automáticas, un proyecto por capa
├── docs/                           Documentación, diagramas y justificación
└── odoo/                           Módulo Python para Odoo (Producto 4)
```

### Responsabilidad de cada capa

- **Modelo:** clases simples que representan los datos. No saben nada de SQL, XML ni pantallas. Es compartida por todas las demás capas.
- **Datos:** guarda y recupera el modelo en SQL Server. Es la única capa que habla con la base de datos.
- **Xml:** lee el fichero XML y lo convierte en objetos del modelo. Es independiente de la base de datos.
- **Negocio:** aplica las reglas de GenteFit sobre el modelo, apoyándose en Datos y Xml.
- **UI:** muestra la información y recoge las acciones del usuario. No accede nunca a la base de datos directamente.

### Lenguajes

- **C#:** modelo, datos, negocio e interfaz.
- **F#:** capa XML.

### Cómo se han creado las capas (a alto nivel)

1. **Solución en blanco** (`GenteFit.sln`) en la raíz del repo: es el contenedor que agrupa todos los proyectos.
2. **Un proyecto por capa**, todos en la carpeta `src/` y con **.NET 8.0**. Cada capa es una *biblioteca de clases* (genera una `.dll`, no se ejecuta sola); la aplicación Windows Forms llegará en el Producto 3.
   - `GenteFit.Modelo` (C#)
   - `GenteFit.Datos` (C#)
   - `GenteFit.Xml` (F#)
3. **Referencias de proyecto** para que las capas se vean entre sí, siempre en una sola dirección: `Datos → Modelo` y `Xml → Modelo`. El Modelo no depende de ninguna otra capa.
4. `.gitignore` (plantilla de Visual Studio) evita subir `bin/`, `obj/` y `.vs/`, y `.gitattributes` unifica los finales de línea entre equipos.

Para trabajar: clonar el repo, abrir `GenteFit.sln` en Visual Studio 2022 y compilar la solución para comprobar que todo funciona.

## Base de datos (SQL Server + EF Core)
 
Cada miembro tiene su propia copia local de la base de datos, lo que se comparte por Git es la estructura (migraciones en `src/GenteFit.Datos/Migrations`).
### Requisitos (solo la primera vez)
 
- **SQL Server 2025 Developer:** para guardar y gestionar la base de datos.
- **SSMS (SQL Server Management Studio):** para ver y manejar la base de datos.
- **.NET 8 SDK:** kit para construir programas en .NET.
- **Herramienta de EF Core (`dotnet-ef` 8.0.11):** utilidad de línea de comandos que añade el comando `dotnet ef`, con el que se crea y actualiza la estructura de la base de datos local (migraciones) y se puede borrar.
### Normas
 
- Nunca cambiar tablas a mano desde SSMS: el cambio no llega al resto y EF se desincroniza. Todo cambio de estructura = cambio en las clases + migración.
- Una sola persona a la vez crea migraciones, para evitar conflictos en `GenteFitContextModelSnapshot.cs`. Avisar antes por el grupo.
- Incluir siempre la carpeta `Migrations` en el commit.
- No borrar ni editar migraciones ya fusionadas; si algo falla, se crea otra que lo corrija.

## Flujo de trabajo en Git

- `main` no se toca: solo recibe `develop` cuando se termina un producto, y siempre debe compilar.
- `develop` es la rama de integración: todo se fusiona aquí.
- Una rama por tarea (`orm`, `xml`, `docs`...), fusionada con *Pull Request* revisado por otro miembro.
- Mensajes de commit cortos que digan qué hace el cambio.
- Las entregas se marcan con etiquetas (p. ej. `producto2-entrega`).
