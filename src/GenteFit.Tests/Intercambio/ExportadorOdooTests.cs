using System.Xml.Linq;
using GenteFit.Modelo.Enums;
using GenteFit.Xml;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace GenteFit.Tests.Intercambio;

// Tests de la capa XML (F#): no usan base de datos, solo datos inventados.
public class ExportadorOdooTests
{
    private static readonly string XsdIda = Path.Combine(EsquemasOdoo.carpeta, "lote_sesiones_ida.xsd");

    private static readonly CabeceraLote Cabecera = new(
        idLote: "GF-2026-11-26-001",
        generado: new DateTime(2026, 11, 26, 10, 30, 0),
        desde: new DateTime(2026, 11, 30),
        hasta: new DateTime(2026, 12, 6));

    // Una sesión válida. Cada test cambia solo lo que necesita.
    private static SesionLote Sesion(string sala = "Sala Montaña", int duracion = 60,
        EstadoSesion estado = EstadoSesion.Programada) =>
        new(gentefitId: Guid.NewGuid(),
            fecha: new DateTime(2026, 11, 30, 19, 0, 0),
            duracion: duracion,
            estado: estado,
            actividad: new Referencia(Guid.NewGuid(), "Pilates Iniciación"),
            intensidad: Intensidad.Baja,
            sala: new Referencia(Guid.NewGuid(), sala),
            entrenador: new Referencia(Guid.NewGuid(), "Laura O'Connor"),
            plazasOcupadas: 12,
            plazasLimite: 16);

    private static FSharpResult<XDocument, FSharpList<string>> Generar(params SesionLote[] sesiones) =>
        ExportadorOdoo.generarLoteIda(XsdIda, Cabecera, ListModule.OfSeq(sesiones));

    [Fact]
    public void Genera_un_lote_valido_con_la_cabecera_y_el_total_correctos()
    {
        var resultado = Generar(Sesion(), Sesion());

        Assert.True(resultado.IsOk);
        var cabecera = resultado.ResultValue.Root!.Element("cabecera")!;
        Assert.Equal("GF-2026-11-26-001", (string?)cabecera.Element("id_lote"));
        Assert.Equal("GenteFit_a_Odoo", (string?)cabecera.Element("sentido"));
        Assert.Equal("2", (string?)cabecera.Element("total_registros"));
        Assert.Equal(2, resultado.ResultValue.Descendants("sesion").Count());
    }

    [Fact]
    public void Las_fechas_van_en_hora_local_sin_zona_horaria()
    {
        var doc = Generar(Sesion()).ResultValue;

        Assert.Equal("2026-11-30T19:00:00", (string?)doc.Descendants("fecha").Single());
        Assert.Equal("2026-11-30", (string?)doc.Descendants("periodo_desde").Single());
    }

    [Fact]
    public void Una_referencia_lleva_el_id_en_el_atributo_y_el_nombre_en_el_texto()
    {
        var sesion = Sesion();
        var sala = Generar(sesion).ResultValue.Descendants("sala").Single();

        Assert.Equal(sesion.Sala.Id.ToString(), (string?)sala.Attribute("id"));
        Assert.Equal("Sala Montaña", sala.Value);
    }

    [Fact]
    public void Conserva_acentos_enye_y_apostrofes_al_pasar_a_bytes()
    {
        var bytes = ExportadorOdoo.aBytes(Generar(Sesion()).ResultValue);
        var leido = XDocument.Load(new MemoryStream(bytes));

        Assert.Equal("Pilates Iniciación", (string?)leido.Descendants("actividad").Single());
        Assert.Equal("Sala Montaña", (string?)leido.Descendants("sala").Single());
        Assert.Equal("Laura O'Connor", (string?)leido.Descendants("entrenador").Single());
    }

    [Fact]
    public void Exporta_las_sesiones_canceladas_con_su_estado()
    {
        var doc = Generar(Sesion(estado: EstadoSesion.Cancelada)).ResultValue;

        Assert.Equal("Cancelada", (string?)doc.Descendants("estado").Single());
    }

    [Fact]
    public void Sin_sesiones_no_se_genera_ningun_lote()
    {
        var resultado = Generar();

        Assert.True(resultado.IsError);
        Assert.Contains("No hay sesiones", Assert.Single(resultado.ErrorValue));
    }

    [Fact]
    public void Un_nombre_de_sala_demasiado_largo_no_cumple_el_XSD()
    {
        var resultado = Generar(Sesion(sala: new string('a', 150)));

        Assert.True(resultado.IsError);
        Assert.Contains(resultado.ErrorValue, e => e.Contains("'sala'"));
    }

    [Fact]
    public void Una_duracion_fuera_de_rango_no_cumple_el_XSD()
    {
        var resultado = Generar(Sesion(duracion: 5));

        Assert.True(resultado.IsError);
        Assert.Contains(resultado.ErrorValue, e => e.Contains("'duracion'"));
    }

    [Fact]
    public void Un_identificador_de_lote_mal_formado_no_cumple_el_XSD()
    {
        var cabecera = new CabeceraLote("LOTE-1", Cabecera.Generado, Cabecera.Desde, Cabecera.Hasta);

        var resultado = ExportadorOdoo.generarLoteIda(XsdIda, cabecera, ListModule.OfSeq([Sesion()]));

        Assert.True(resultado.IsError);
        Assert.Contains(resultado.ErrorValue, e => e.Contains("'id_lote'"));
    }

    [Fact]
    public void Si_no_encuentra_el_XSD_devuelve_un_error_claro()
    {
        var resultado = ExportadorOdoo.generarLoteIda("no_existe.xsd", Cabecera, ListModule.OfSeq([Sesion()]));

        Assert.True(resultado.IsError);
        Assert.Contains("No se encuentra el esquema XSD", Assert.Single(resultado.ErrorValue));
    }
}