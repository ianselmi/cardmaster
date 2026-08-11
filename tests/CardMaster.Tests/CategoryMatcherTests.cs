using CardMaster.Data;
using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Classificazione di una riga: confronto per token e prefisso sul dizionario, con le
/// mappature apprese davanti a tutto.
/// </summary>
public class CategoryMatcherTests
{
    private static readonly List<ProductCategory> Catalog =
    [
        new() { Id = "dispensa", Name = "Dispensa", Keywords = ["pasta", "riso", "olio"] },
        new() { Id = "ortofrutta", Name = "Ortofrutta", Keywords = ["mele", "carote", "zucchine"] },
        new() { Id = "colazione", Name = "Colazione e dolci", Keywords = ["miele", "biscotti"] },
        new() { Id = "latticini", Name = "Latticini", Keywords = ["latte", "mozzarella"] },
        new() { Id = "cura-casa", Name = "Cura della casa", Keywords = ["carta igienica", "detersivo"] },
    ];

    [Fact]
    public void Corrispondenza_per_token()
    {
        Assert.Equal("ortofrutta", CategoryMatcher.Match("CAROTE KG 1", Catalog));
    }

    [Fact]
    public void Corrispondenza_per_prefisso_su_abbreviazione()
    {
        Assert.Equal("dispensa", CategoryMatcher.Match("PAST. BARILLA 500", Catalog));
    }

    /// <summary>
    /// Il caso che giustifica la scelta di non usare una distanza di edit: <c>MELE</c> e
    /// <c>MIELE</c> distano un carattere e stanno in due categorie diverse.
    /// </summary>
    [Fact]
    public void Parole_simili_ma_diverse_non_si_confondono()
    {
        Assert.Equal("ortofrutta", CategoryMatcher.Match("MELE GOLDEN", Catalog));
        Assert.Equal("colazione", CategoryMatcher.Match("MIELE MILLEFIORI", Catalog));
    }

    [Fact]
    public void Prefisso_troppo_corto_non_basta()
    {
        // "pas" prefissa "pasta" ma è ambiguo: tre lettere non identificano un prodotto.
        Assert.Null(CategoryMatcher.Match("PAS", Catalog));
    }

    [Fact]
    public void Parola_chiave_composta_riconosciuta_come_frase()
    {
        Assert.Equal("cura-casa", CategoryMatcher.Match("CARTA IGIENICA 4 ROTOLI", Catalog));
    }

    [Fact]
    public void Nessuna_corrispondenza_lascia_la_riga_senza_categoria()
    {
        Assert.Null(CategoryMatcher.Match("ARTICOLO SCONOSCIUTO XZ", Catalog));
    }

    [Fact]
    public void Descrizione_vuota_non_classificata()
    {
        Assert.Null(CategoryMatcher.Match("   ", Catalog));
    }

    [Fact]
    public void Mappatura_appresa_vince_sul_dizionario()
    {
        var learned = new Dictionary<string, string> { ["latte di mandorla"] = "dispensa" };

        Assert.Equal("dispensa", CategoryMatcher.Resolve("LATTE DI MANDORLA", learned, Catalog));
        Assert.Equal("latticini", CategoryMatcher.Match("LATTE DI MANDORLA", Catalog));
    }

    [Fact]
    public void Senza_mappatura_appresa_vale_il_dizionario()
    {
        var learned = new Dictionary<string, string>();

        Assert.Equal("latticini", CategoryMatcher.Resolve("LATTE INTERO", learned, Catalog));
    }

    [Fact]
    public void Accenti_e_maiuscole_non_contano()
    {
        var learned = new Dictionary<string, string> { ["caffe borbone"] = "dispensa" };

        Assert.Equal("dispensa", CategoryMatcher.Resolve("Caffè Borbone", learned, Catalog));
    }
}
