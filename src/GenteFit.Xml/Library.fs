namespace GenteFit.Xml
 
open System
open System.IO
open System.Xml
open System.Xml.Linq
open GenteFit.Modelo.Entidades
open GenteFit.Modelo.Enums
 
 /// Escrito en F#. Contiene toda la lógica para leer y escribir ficheros XML.
type Lectura<'T> =
    { Elementos: 'T list
      Avisos: string list }
 
///LECTOR
module LectorXml =
  
    /// Abre el fichero XML. Si falla, devuelve un mensaje claro en vez de romper el programa.
    let private cargar (ruta: string) : Result<XDocument, string> =
        try
            Ok(XDocument.Load ruta)
        with
        | :? FileNotFoundException -> Error $"No se encuentra el fichero XML: {Path.GetFileName(ruta)}"
        | :? DirectoryNotFoundException -> Error $"No se encuentra la carpeta del fichero XML: {ruta}"
        | :? XmlException as ex -> Error $"El XML está mal formado (línea {ex.LineNumber}, posición {ex.LinePosition})."
        | :? IOException as ex -> Error $"No se puede leer el fichero XML: {ex.Message}"
        | :? UnauthorizedAccessException -> Error $"Sin permiso para leer el fichero XML: {ruta}"
 
    /// Devuelve el texto de un elemento hijo, o None si no existe o está vacío.
    let private texto (padre: XElement) (nombre: string) : string option =
        match padre.Element(XName.Get nombre) with
        | null -> None
        | hijo ->
            let valor = hijo.Value.Trim()
            if valor = "" then None else Some valor
 
    /// Convierte un texto en un número entero, o None si no lo es.
    let private aEntero (valor: string) : int option =
        match Int32.TryParse valor with
        | true, n -> Some n
        | _ -> None
 
    /// Convierte un texto en un valor de un enum (ignorando mayúsculas), o None si no existe.
    let private aEnum<'E when 'E: struct and 'E :> ValueType and 'E: (new: unit -> 'E)> (valor: string) : 'E option =
        match Enum.TryParse<'E>(valor, true) with
        | true, v when Enum.IsDefined(typeof<'E>, v) -> Some v
        | _ -> None
 
    // ---------- Actividad ---------- 
    /// Convierte un elemento <actividad> en un objeto Actividad.
    /// Devuelve Error con el motivo si falta algo obligatorio o tiene un valor no válido.
    let private leerActividad (e: XElement) : Result<Actividad, string> =
        let nombre = texto e "nombre"
        let especialidad = texto e "especialidad" |> Option.bind aEnum<Especialidad>
        let intensidad = texto e "intensidad" |> Option.bind aEnum<Intensidad>
 
        match nombre, especialidad, intensidad with
        | Some n, Some esp, Some inten ->
            // Estos campos son opcionales: si faltan, se usan valores por defecto.
            let limite = texto e "limiteUsuarios" |> Option.bind aEntero |> Option.defaultValue 16
 
            Ok(
                Actividad(
                    ActividadId = Guid.NewGuid(),
                    NombreActividad = n,
                    Especialidad = esp,
                    Intensidad = inten,
                    LimiteUsuarios = limite,
                    Descripcion = (texto e "descripcion" |> Option.toObj),
                    UrlImagen = (texto e "urlImagen" |> Option.toObj)
                )
            )
        | _ ->
            let problemas =
                [ if nombre.IsNone then "falta <nombre>"
                  if especialidad.IsNone then "<especialidad> ausente o desconocida"
                  if intensidad.IsNone then "<intensidad> ausente o desconocida" ]
 
            Error(sprintf "Actividad '%s' ignorada: %s." (defaultArg nombre "?") (String.Join(", ", problemas)))
 
    /// Lee todas las actividades del fichero XML.
    /// Error: el fichero no se pudo abrir. Ok: lista de actividades válidas y avisos de las ignoradas.
    let leerActividades (ruta: string) : Result<Lectura<Actividad>, string> =
        cargar ruta
        |> Result.map (fun doc ->
            let resultados =
                doc.Descendants(XName.Get "actividad") |> Seq.map leerActividad |> Seq.toList
 
            { Elementos = resultados |> List.choose (function Ok a -> Some a | Error _ -> None)
              Avisos = resultados |> List.choose (function Error m -> Some m | Ok _ -> None) })

    // ---------- Usuario (cliente) ----------
    /// Convierte un texto en true/false, o None si no es un valor válido.
    let private aBool (valor: string) : bool option =
        match Boolean.TryParse valor with
        | true, b -> Some b
        | _ -> None

    /// Convierte un elemento <usuario> en un objeto Usuario.
    /// Devuelve Error con el motivo si falta algo obligatorio o tiene un valor no válido.
    let private leerUsuario (e: XElement) : Result<Usuario, string> =
        let nombre = texto e "nombre"
        let apellido1 = texto e "apellido1"
        let dni = texto e "dni"
        let email = texto e "email"
        let telefono = texto e "telefono" |> Option.bind aEntero
        let rol = texto e "rol" |> Option.bind aEnum<Rol>

        match nombre, apellido1, dni, email, telefono, rol with
        | Some n, Some a1, Some d, Some em, Some tel, Some r ->
            // <activo> y <apellido2> son opcionales.
            let activo = texto e "activo" |> Option.bind aBool |> Option.defaultValue true

            Ok(
                Usuario(
                    UsuarioId = Guid.NewGuid(),
                    Nombre = n,
                    Apellido1 = a1,
                    Apellido2 = (texto e "apellido2" |> Option.toObj),
                    Dni = d,
                    Email = em,
                    Telefono = tel,
                    Rol = r,
                    Activo = activo
                )
            )
        | _ ->
            let problemas =
                [ if nombre.IsNone then "falta <nombre>"
                  if apellido1.IsNone then "falta <apellido1>"
                  if dni.IsNone then "falta <dni>"
                  if email.IsNone then "falta <email>"
                  if telefono.IsNone then "<telefono> ausente o no numérico"
                  if rol.IsNone then "<rol> ausente o desconocido" ]

            Error(sprintf "Usuario '%s' ignorado: %s." (defaultArg email (defaultArg nombre "?")) (String.Join(", ", problemas)))

    /// Lee todos los usuarios del fichero XML.
    /// Error: el fichero no se pudo abrir. Ok: lista de usuarios válidos y avisos de los ignorados.
    let leerUsuarios (ruta: string) : Result<Lectura<Usuario>, string> =
        cargar ruta
        |> Result.map (fun doc ->
            let resultados =
                doc.Descendants(XName.Get "usuario") |> Seq.map leerUsuario |> Seq.toList

            { Elementos = resultados |> List.choose (function Ok u -> Some u | Error _ -> None)
              Avisos = resultados |> List.choose (function Error m -> Some m | Ok _ -> None) })

/// ESCRITOR
module EscritorXml =

    /// Crea un elemento <nombre>valor</nombre>.
    let private elemento (nombre: string) (valor: string) : XElement =
        XElement(XName.Get nombre, valor)

    /// Convierte un Usuario en su elemento <usuario>. No se exporta la contraseña.
    let private usuarioAXml (u: Usuario) : XElement =
        let hijos =
            [ elemento "nombre" u.Nombre
              elemento "apellido1" u.Apellido1
              if not (isNull u.Apellido2) then elemento "apellido2" u.Apellido2
              elemento "dni" u.Dni
              elemento "email" u.Email
              elemento "telefono" (string u.Telefono)
              elemento "rol" (u.Rol.ToString().ToUpperInvariant())
              elemento "activo" (if u.Activo then "true" else "false") ]

        XElement(XName.Get "usuario", hijos)

    /// Guarda una lista de usuarios en un fichero XML (UTF-8).
    /// Ok: número de usuarios escritos. Error: motivo por el que no se pudo guardar.
    let escribirUsuarios (ruta: string) (usuarios: Usuario seq) : Result<int, string> =
        try
            let lista = Seq.toList usuarios

            // Se monta el documento pieza a pieza: <gentefit><usuarios>...</usuarios></gentefit>
            let nodoUsuarios = XElement(XName.Get "usuarios")
            for u in lista do
                nodoUsuarios.Add(usuarioAXml u)

            let raiz = XElement(XName.Get "gentefit")
            raiz.Add(nodoUsuarios)

            let doc = XDocument()
            doc.Declaration <- XDeclaration("1.0", "UTF-8", null)
            doc.Add(raiz)
            doc.Save(ruta)

            Ok lista.Length
        with
        | :? DirectoryNotFoundException -> Error $"No existe la carpeta donde guardar el XML: {ruta}"
        | :? UnauthorizedAccessException -> Error $"Sin permiso para escribir el fichero XML: {ruta}"
        | :? IOException as ex -> Error $"No se puede escribir el fichero XML: {ex.Message}"