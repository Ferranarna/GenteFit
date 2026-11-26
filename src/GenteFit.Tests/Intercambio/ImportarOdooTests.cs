using System.Text;
using GenteFit.Modelo.Enums;
using GenteFit.Xml;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace GenteFit.Tests.Intercambio;

// Lectura del lote de vuelta con los ficheros de odoo/ejemplos (los mismos que usa Odoo).
public class ImportadorOdooTests
{
    private static readonly string XsdVuelta = Path.Combine(EsquemasOdoo.carpeta, "lote_sesiones_vuelta.xsd");

    private static string Ejemplo(string fichero) => Path.Combine(AppContext.BaseDirectory, "ejemplos", fichero);

    private static FSharpResult<LoteRecibido, FSharpList<string>> Leer(Stream contenido) =>
        ImportadorOdoo.leerLoteVuelta(XsdVuelta, contenido);

    private static FSharpResult<LoteRecibido, FSharpList<string>> LeerEjemplo(string fichero)
    {
        using var contenido = File.OpenRead(Ejemplo(fichero));
        return Leer(contenido);
    }

    [Fact]
    public void Lee_el_lote_valido_con_sus_tres_sesiones()
    {
        var resultado = LeerEjemplo("vuelta_valido.xml");

        Assert.True(resultado.IsOk);
        var lote = resultado.ResultValue;
        Assert.Equal("OD-2026-11-26-001", lote.IdLote);
        Assert.Equal(new DateTime(2026, 11, 30), lote.Desde);
        Assert.Equal(
            [EstadoSesion.Programada, EstadoSesion.Cancelada, EstadoSesion.Programada],
            lote.Sesiones.Select(s => s.Estado));

        var primera = lote.Sesiones.Head;
        Assert.Equal(new DateTime(2026, 11, 30, 20, 0, 0), primera.Fecha);
        Assert.Equal(Guid.Parse("d84b0e2c-5a1f-4e93-b7c6-3e2f9a1d6b40"), primera.Sala.Id);
        Assert.Equal("Sala Montaña", primera.Sala.Nombre);
        Assert.Equal("Laura O'Connor", primera.Entrenador.Nombre);
    }

    [Theory]
    [InlineData("ida_err_mal_formado.xml", "no es un XML correcto")]     // nivel A: etiqueta sin cerrar
    [InlineData("ida_err_codificacion.xml", "no es un XML correcto")]    // nivel A: bytes que no son UTF-8
    [InlineData("vuelta_err_duracion.xml", "'duracion'")]                // nivel B: 200 minutos
    [InlineData("ida_valido.xml", "'id_lote'")]                          // nivel B: un lote de ida no es de vuelta
    public void Rechaza_el_lote_entero_si_el_fichero_no_es_correcto(string fichero, string motivo)
    {
        var resultado = LeerEjemplo(fichero);

        Assert.True(resultado.IsError);
        Assert.Contains(resultado.ErrorValue, e => e.Contains(motivo));
    }

    [Fact]
    public void Los_errores_del_XSD_indican_la_linea()
    {
        var resultado = LeerEjemplo("vuelta_err_duracion.xml");

        Assert.StartsWith("Línea ", Assert.Single(resultado.ErrorValue));
    }

    [Fact]
    public void Rechaza_el_lote_si_el_total_no_coincide_con_las_sesiones()
    {
        // Se simula un fichero cortado: dice 4 sesiones pero trae 3
        var texto = File.ReadAllText(Ejemplo("vuelta_valido.xml"))
            .Replace("<total_registros>3</total_registros>", "<total_registros>4</total_registros>");
        using var contenido = new MemoryStream(Encoding.UTF8.GetBytes(texto));

        var resultado = Leer(contenido);

        Assert.True(resultado.IsError);
        Assert.Contains("dice tener 4 sesiones pero contiene 3", Assert.Single(resultado.ErrorValue));
    }

    [Theory]
    [InlineData("vuelta_err_ref_inexistente.xml")]
    [InlineData("vuelta_err_solapamiento.xml")]
    public void Los_errores_de_negocio_no_rechazan_el_fichero(string fichero)
    {
        // El fichero es correcto: la actividad inexistente o el solapamiento los decide el repositorio
        var resultado = LeerEjemplo(fichero);

        Assert.True(resultado.IsOk);
        Assert.Equal(3, resultado.ResultValue.Sesiones.Length);
    }
}