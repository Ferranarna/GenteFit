using GenteFit.Xml;

namespace GenteFit.Tests.Intercambio;

// Historial de lotes: se leen las cabeceras de los ficheros guardados en una carpeta temporal.
public class HistorialOdooTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "gentefit-lotes-" + Guid.NewGuid());

    public HistorialOdooTests() => Directory.CreateDirectory(_carpeta);

    public void Dispose() => Directory.Delete(_carpeta, recursive: true);

    private void Copiar(string ejemplo) =>
        File.Copy(Path.Combine(AppContext.BaseDirectory, "ejemplos", ejemplo), Path.Combine(_carpeta, ejemplo));

    [Fact]
    public void Lista_los_lotes_del_mas_reciente_al_mas_antiguo()
    {
        Copiar("ida_valido.xml");      // GF-..., generado el 25/11
        Copiar("vuelta_valido.xml");   // OD-..., generado el 26/11

        var lotes = HistorialOdoo.listar(_carpeta);

        Assert.Equal(["OD-2026-11-26-001", "GF-2026-11-25-001"], lotes.Select(l => l.IdLote));
        Assert.False(lotes.Head.Enviado);
        Assert.True(lotes[1].Enviado);
        Assert.Equal(2, lotes[1].Sesiones);
    }

    [Fact]
    public void Un_fichero_danado_no_rompe_el_historial()
    {
        Copiar("vuelta_valido.xml");
        Copiar("ida_err_mal_formado.xml");
        File.WriteAllText(Path.Combine(_carpeta, "notas.xml"), "<otra_cosa/>");   // XML que no es un lote

        var lote = Assert.Single(HistorialOdoo.listar(_carpeta));

        Assert.Equal("OD-2026-11-26-001", lote.IdLote);
    }

    [Fact]
    public void Si_la_carpeta_no_existe_el_historial_esta_vacio()
    {
        Assert.Empty(HistorialOdoo.listar(Path.Combine(_carpeta, "no_existe")));
    }
}