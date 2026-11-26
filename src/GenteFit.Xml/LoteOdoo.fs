namespace GenteFit.Xml

open System
open System.Globalization
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq
open System.Xml.Schema
open GenteFit.Modelo.Enums

/// Referencia a otra entidad de GenteFit: Id + nombre.
type Referencia = { Id: Guid; Nombre: string }

/// Una sesión tal y como viaja en el lote. No depende de EF Core.
type SesionLote =
    { GentefitId: Guid
      Fecha: DateTime
      Duracion: int
      Estado: EstadoSesion
      Actividad: Referencia
      Intensidad: Intensidad
      Sala: Referencia
      Entrenador: Referencia
      PlazasOcupadas: int
      PlazasLimite: int }

/// Parámetros que identifican el lote.
type CabeceraLote =
    { IdLote: string
      Generado: DateTime
      Desde: DateTime
      Hasta: DateTime }

/// Carga los esquemas XSD del intercambio y valida documentos con ellos.
module EsquemasOdoo =

    /// Carpeta donde se copian los XSD al compilar (ver GenteFit.Xml.fsproj).
    let carpeta = Path.Combine(AppContext.BaseDirectory, "xsd")

    /// Carga un XSD (y los que incluya). Error: motivo por el que no se pudo cargar.
    let cargar (rutaXsd: string) : Result<XmlSchemaSet, string> =
        try
            let esquemas = XmlSchemaSet()
            // Necesario para que se resuelva <xs:include schemaLocation="tipos_comunes.xsd"/>
            esquemas.XmlResolver <- XmlUrlResolver()
            esquemas.Add(null, rutaXsd) |> ignore
            esquemas.Compile()
            Ok esquemas
        with
        | :? FileNotFoundException
        | :? DirectoryNotFoundException -> Error $"No se encuentra el esquema XSD: {Path.GetFileName rutaXsd}"
        | :? XmlSchemaException as ex -> Error $"El esquema XSD tiene errores: {ex.Message}"
        | :? XmlException as ex -> Error $"El esquema XSD está mal formado: {ex.Message}"

    /// Valida un documento. Devuelve la lista de errores (vacía si es válido).
    let validar (esquemas: XmlSchemaSet) (doc: XDocument) : string list =
        let errores = ResizeArray<string>()
        doc.Validate(
            esquemas,
            ValidationEventHandler(fun _ e ->
                // Si el documento se cargó con números de línea, se indican para localizar el error
                let linea = e.Exception.LineNumber
                errores.Add(if linea > 0 then $"Línea {linea}: {e.Message}" else e.Message))
        )
        List.ofSeq errores

/// Genera el lote de ida (GenteFit -> Odoo).
module ExportadorOdoo =

    // Fechas en ISO 8601 sin zona horaria: hora local de España, como la guarda GenteFit.
    let private fechaHora (d: DateTime) =
        d.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)

    let private fecha (d: DateTime) =
        d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

    /// Crea <nombre>valor</nombre>.
    let private elemento (nombre: string) (valor: string) =
        XElement(XName.Get nombre, valor)

    /// Crea <nombre id="GUID">Nombre</nombre>.
    let private referencia (nombre: string) (r: Referencia) =
        XElement(XName.Get nombre, XAttribute(XName.Get "id", r.Id), r.Nombre)

    let private sesionAXml (s: SesionLote) =
        XElement(
            XName.Get "sesion",
            elemento "gentefit_id" (string s.GentefitId),
            elemento "fecha" (fechaHora s.Fecha),
            elemento "duracion" (string s.Duracion),
            elemento "estado" (string s.Estado),
            referencia "actividad" s.Actividad,
            elemento "intensidad" (string s.Intensidad),
            referencia "sala" s.Sala,
            referencia "entrenador" s.Entrenador,
            elemento "plazas_ocupadas" (string s.PlazasOcupadas),
            elemento "plazas_limite" (string s.PlazasLimite)
        )

    /// Construye el documento del lote de ida, sin validar.
    let crearLoteIda (cab: CabeceraLote) (sesiones: SesionLote list) : XDocument =
        let cabecera =
            XElement(
                XName.Get "cabecera",
                elemento "id_lote" cab.IdLote,
                elemento "sentido" "GenteFit_a_Odoo",
                elemento "generado" (fechaHora cab.Generado),
                elemento "periodo_desde" (fecha cab.Desde),
                elemento "periodo_hasta" (fecha cab.Hasta),
                elemento "total_registros" (string sesiones.Length)
            )

        let nodoSesiones = XElement(XName.Get "sesiones", sesiones |> List.map sesionAXml)

        let raiz =
            XElement(XName.Get "lote_sesiones", XAttribute(XName.Get "version", "1.0"), cabecera, nodoSesiones)

        let doc = XDocument(raiz)
        doc.Declaration <- XDeclaration("1.0", "UTF-8", null)
        doc

    /// Genera el lote y lo valida con el XSD.
    /// Ok: documento válido. Error: lista de motivos por los que no se ha generado.
    let generarLoteIda (rutaXsd: string) (cab: CabeceraLote) (sesiones: SesionLote list) : Result<XDocument, string list> =
        if List.isEmpty sesiones then
            Error [ "No hay sesiones en el periodo elegido: no se ha generado ningún lote." ]
        else
            match EsquemasOdoo.cargar rutaXsd with
            | Error motivo -> Error [ motivo ]
            | Ok esquemas ->
                let doc = crearLoteIda cab sesiones

                match EsquemasOdoo.validar esquemas doc with
                | [] -> Ok doc
                | errores -> Error errores

    /// Convierte el documento en bytes UTF-8 con sangría, listos para descargar o guardar.
    let aBytes (doc: XDocument) : byte[] =
        let ajustes = XmlWriterSettings(Encoding = UTF8Encoding(false), Indent = true)
        use memoria = new MemoryStream()
        let escritor = XmlWriter.Create(memoria, ajustes)
        doc.Save(escritor)
        escritor.Close() // vuelca lo escrito a la memoria
        memoria.ToArray()

        /// Una sesión recibida de Odoo (lote de vuelta).
type SesionRecibida =
    { GentefitId: Guid
      Fecha: DateTime
      Duracion: int
      Estado: EstadoSesion
      Actividad: Referencia
      Sala: Referencia
      Entrenador: Referencia }

/// Lote de vuelta ya leído y validado.
type LoteRecibido =
    { IdLote: string
      Generado: DateTime
      Desde: DateTime
      Hasta: DateTime
      Sesiones: SesionRecibida list }

/// Lee el lote de vuelta (Odoo -> GenteFit).
module ImportadorOdoo =

    let private valor (padre: XElement) (nombre: string) = padre.Element(XName.Get nombre).Value

    let private fechaHora (texto: string) =
        XmlConvert.ToDateTime(texto, XmlDateTimeSerializationMode.RoundtripKind)

    let private referencia (padre: XElement) (nombre: string) =
        let e = padre.Element(XName.Get nombre)
        { Id = Guid.Parse(e.Attribute(XName.Get "id").Value); Nombre = e.Value.Trim() }

    let private leerSesion (e: XElement) : SesionRecibida =
        { GentefitId = Guid.Parse(valor e "gentefit_id")
          Fecha = fechaHora (valor e "fecha")
          Duracion = int (valor e "duracion")
          Estado = Enum.Parse<EstadoSesion>(valor e "estado")
          Actividad = referencia e "actividad"
          Sala = referencia e "sala"
          Entrenador = referencia e "entrenador" }

    /// Carga el XML. Nivel A: si no está bien formado, Error con la línea y la posición.
    let private cargar (contenido: Stream) : Result<XDocument, string> =
        try
            Ok(XDocument.Load(contenido, LoadOptions.SetLineInfo))
        with :? XmlException as ex ->
            Error $"El fichero no es un XML correcto (línea {ex.LineNumber}, posición {ex.LinePosition}). Puede estar incompleto o no estar guardado en UTF-8."

    /// Lee y valida un lote de vuelta.
    /// Ok: lote listo para aplicar. Error: motivos por los que se rechaza el lote entero.
    let leerLoteVuelta (rutaXsd: string) (contenido: Stream) : Result<LoteRecibido, string list> =
        match cargar contenido with
        | Error motivo -> Error [ motivo ]
        | Ok doc ->
            match EsquemasOdoo.cargar rutaXsd with
            | Error motivo -> Error [ motivo ]
            | Ok esquemas ->
                match EsquemasOdoo.validar esquemas doc with
                | _ :: _ as errores -> Error errores // nivel B: no cumple el XSD
                | [] ->
                    // Ya es válido: los elementos existen y tienen el formato correcto
                    let cabecera = doc.Root.Element(XName.Get "cabecera")
                    let sesiones =
                        doc.Root.Element(XName.Get "sesiones").Elements(XName.Get "sesion")
                        |> Seq.map leerSesion
                        |> Seq.toList
                    let total = int (valor cabecera "total_registros")

                    if total <> sesiones.Length then
                        // Nivel C de lote: el fichero se ha cortado o manipulado
                        Error [ $"El lote dice tener {total} sesiones pero contiene {sesiones.Length}. Puede estar incompleto: pide que se vuelva a generar." ]
                    else
                        Ok
                            { IdLote = valor cabecera "id_lote"
                              Generado = fechaHora (valor cabecera "generado")
                              Desde = DateTime.Parse(valor cabecera "periodo_desde", CultureInfo.InvariantCulture)
                              Hasta = DateTime.Parse(valor cabecera "periodo_hasta", CultureInfo.InvariantCulture)
                              Sesiones = sesiones }