using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Ricostruzione delle righe prodotto dalla geometria dell'OCR.
/// <para>
/// I casi sono costruiti con le <b>coordinate</b> dei frammenti, non con testo già in colonne,
/// perché è esattamente la geometria la cosa sotto test: uno scontrino in cui descrizione e
/// prezzo arrivano come stringhe già accoppiate non proverebbe niente.
/// </para>
/// </summary>
public class ReceiptItemsParserTests
{
    private const double DescriptionX = 10;
    private const double VatX = 250;
    private const double PriceX = 330;

    /// <summary>
    /// Costruisce uno scontrino a colonne come lo restituisce ML Kit: frammenti sparsi con le
    /// loro coordinate, che solo il layout rimette in riga.
    /// </summary>
    private sealed class ReceiptBuilder
    {
        private readonly List<OcrLine> _fragments = [];
        private double _y;

        /// <summary>Riga di testo libero (intestazione, coda, continuazione).</summary>
        public ReceiptBuilder Text(string text, double x = DescriptionX)
        {
            Add(text, x, 200);
            return Next();
        }

        /// <summary>Riga prodotto: descrizione, aliquota in colonna (opzionale), prezzo.</summary>
        public ReceiptBuilder Row(string description, string? vat, string price)
        {
            Add(description, DescriptionX, 200);
            if (vat is not null)
            {
                Add(vat, VatX, 45);
            }

            Add(price, PriceX, 55);
            return Next();
        }

        /// <summary>Riga con il prezzo, ma la descrizione spezzata in più frammenti.</summary>
        public ReceiptBuilder RowParts(string price, params (string Text, double X, double Width)[] parts)
        {
            foreach (var part in parts)
            {
                Add(part.Text, part.X, part.Width);
            }

            Add(price, PriceX, 55);
            return Next();
        }

        private void Add(string text, double x, double width) =>
            _fragments.Add(new OcrLine(text, new Rect(x, _y, width, 18)));

        private ReceiptBuilder Next()
        {
            _y += 30;
            return this;
        }

        /// <summary>
        /// I frammenti escono <b>rimescolati per colonna</b>, come fa il motore vero: prima
        /// tutte le descrizioni, poi tutti i prezzi. Se il parser funzionasse per ordine del
        /// testo invece che per posizione, questi test lo scoprirebbero.
        /// </summary>
        public OcrResult Build()
        {
            var byColumn = _fragments.OrderBy(f => f.Bounds.Left).ThenBy(f => f.Bounds.Top).ToList();
            var text = string.Join("\n", byColumn.Select(f => f.Text));
            return new OcrResult(text, [new OcrBlock(text, new Rect(0, 0, 400, _y), byColumn)]);
        }

        public IReadOnlyList<ReceiptVisualLine> Layout() => ReceiptTextLayout.ToVisualLayout(Build());
    }

    private static ReceiptBuilder Receipt() => new ReceiptBuilder().Text("MD DISCOUNT SRL");

    [Fact]
    public void Separa_descrizione_e_prezzo_per_colonna()
    {
        var layout = Receipt()
            .Row("TOFU BIO", null, "1,49")
            .Row("CAROTE KG 1", null, "1,19")
            .Text("TOTALE COMPLESSIVO                 2,68")
            .Layout();

        var result = ReceiptItemsParser.Parse(layout);

        Assert.True(result.BodyFound);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("TOFU BIO", result.Items[0].RawDescription);
        Assert.Equal(149, result.Items[0].AmountCents);
        Assert.Equal("CAROTE KG 1", result.Items[1].RawDescription);
        Assert.Equal(119, result.Items[1].AmountCents);
    }

    [Fact]
    public void Numero_nella_descrizione_non_diventa_prezzo()
    {
        var layout = Receipt()
            .RowParts("4,50", ("PROSCIUTTO", DescriptionX, 120), ("100 GR", 140, 60))
            .Row("TOFU BIO", null, "1,49")
            .Text("TOTALE                             5,99")
            .Layout();

        var result = ReceiptItemsParser.Parse(layout);

        Assert.Equal("PROSCIUTTO 100 GR", result.Items[0].RawDescription);
        Assert.Equal(450, result.Items[0].AmountCents);
    }

    [Fact]
    public void Quantita_esplicita_sulla_stessa_riga()
    {
        var layout = Receipt()
            .RowParts("1,78", ("MOZZARELLA", DescriptionX, 120), ("2 X 0,89", 140, 80))
            .Text("TOTALE                             1,78")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal(2000, item.QuantityMilli);
        Assert.Equal(ReceiptItemUnit.Piece, item.Unit);
        Assert.Equal(89, item.UnitPriceCents);
        Assert.Equal(178, item.AmountCents);
        Assert.False(item.IsInconsistent);
    }

    [Fact]
    public void Quantita_su_riga_separata_si_attacca_al_prodotto()
    {
        var layout = Receipt()
            .Row("YOGURT SKYR BIANCO", null, "1,18")
            .Text("2 X 0,59")
            .Text("TOTALE                             1,18")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal("YOGURT SKYR BIANCO", item.RawDescription);
        Assert.Equal(2000, item.QuantityMilli);
        Assert.Equal(59, item.UnitPriceCents);
        Assert.Equal(118, item.AmountCents);
    }

    [Fact]
    public void Totale_di_riga_incoerente_viene_segnalato_non_corretto()
    {
        var layout = Receipt()
            .RowParts("1,88", ("MOZZARELLA", DescriptionX, 120), ("2 X 0,89", 140, 80))
            .Text("TOTALE                             1,88")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.True(item.IsInconsistent);
        Assert.Equal(188, item.AmountCents);
    }

    [Fact]
    public void Prodotto_a_peso_in_millesimi()
    {
        var layout = Receipt()
            .Row("ZUCCHINE SCURE", null, "0,92")
            .Text("0,546 kg x 1,69 €/kg")
            .Text("TOTALE                             0,92")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal(546, item.QuantityMilli);
        Assert.Equal(ReceiptItemUnit.Kilogram, item.Unit);
        Assert.Equal(169, item.UnitPriceCents);
        Assert.Equal(92, item.AmountCents);
        Assert.False(item.IsInconsistent);
    }

    [Fact]
    public void Sconto_con_importo_negativo_non_e_un_prodotto()
    {
        var layout = Receipt()
            .Row("MINI BITES THAI", null, "1,99")
            .Row("SCONTO 50%", null, "-1,00")
            .Text("TOTALE                             0,99")
            .Layout();

        var items = ReceiptItemsParser.Parse(layout).Items;

        Assert.Equal(ReceiptItemKind.Product, items[0].Kind);
        Assert.Equal(ReceiptItemKind.Discount, items[1].Kind);
        Assert.Equal(-100, items[1].AmountCents);
    }

    [Fact]
    public void Sconto_stampato_senza_segno_resta_negativo()
    {
        var layout = Receipt()
            .Row("MINI BITES THAI", null, "1,99")
            .Row("SCONTO OFFERTA VOLANTINO", null, "0,08")
            .Text("TOTALE                             1,91")
            .Layout();

        var items = ReceiptItemsParser.Parse(layout).Items;

        Assert.Equal(-8, items[1].AmountCents);
    }

    [Fact]
    public void Descrizione_a_capo_non_genera_un_prodotto_fantasma()
    {
        var layout = Receipt()
            .Row("AFFETTATO VEGETALE", null, "1,49")
            .Text("GUSTO COTTO")
            .Text("TOTALE                             1,49")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal("AFFETTATO VEGETALE GUSTO COTTO", item.RawDescription);
        Assert.Equal(149, item.AmountCents);
    }

    [Fact]
    public void Riga_di_servizio_scartata()
    {
        var layout = Receipt()
            .Row("TOFU BIO", null, "1,49")
            .Row("REPARTO 3", null, "1,49")
            .Text("TOTALE                             1,49")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal("TOFU BIO", item.RawDescription);
    }

    [Fact]
    public void Coda_dopo_il_totale_esclusa()
    {
        var layout = Receipt()
            .Row("TOFU BIO", null, "1,49")
            .Text("TOTALE                             1,49")
            .Row("CONTANTE", null, "5,00")
            .Row("RESTO", null, "3,51")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal("TOFU BIO", item.RawDescription);
    }

    [Fact]
    public void Corpo_delimitato_dal_subtotale_quando_precede_il_totale()
    {
        var layout = Receipt()
            .Row("TOFU BIO", null, "1,49")
            .Text("SUBTOTALE                          1,49")
            .Row("BUONO SCONTO FEDELTA", null, "0,50")
            .Text("TOTALE                             0,99")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal("TOFU BIO", item.RawDescription);
    }

    [Fact]
    public void Senza_riga_del_totale_nessuna_riga_proposta()
    {
        var layout = Receipt()
            .Row("TOFU BIO", null, "1,49")
            .Row("CAROTE", null, "1,19")
            .Layout();

        var result = ReceiptItemsParser.Parse(layout);

        Assert.False(result.BodyFound);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void Intestazione_esclusa()
    {
        var layout = new ReceiptBuilder()
            .Text("MD SPA")
            .Text("VIA ROMA 12 - RIVERGARO (PC)")
            .Text("P.IVA 12345678901")
            .Row("TOFU BIO", null, "1,49")
            .Text("TOTALE                             1,49")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal("TOFU BIO", item.RawDescription);
    }

    [Fact]
    public void Aliquota_letta_dalla_colonna()
    {
        var layout = Receipt()
            .Row("PRIMOSALE", "4,00", "2,00")
            .Row("MORTADELLA", "10,00", "1,39")
            .Row("SPRAY VETRI", "22,00", "1,29")
            .Text("TOTALE                             4,68")
            .Layout();

        var items = ReceiptItemsParser.Parse(layout).Items;

        Assert.Equal([400, 1000, 2200], items.Select(i => i.VatRateBasisPoints).ToArray());
        Assert.Equal([200, 139, 129], items.Select(i => i.AmountCents).ToArray());
        Assert.Equal("PRIMOSALE", items[0].RawDescription);
    }

    [Fact]
    public void Codice_di_reparto_risolto_dal_riepilogo()
    {
        var layout = Receipt()
            .Row("PRIMOSALE", "1", "2,00")
            .Row("MORTADELLA", "2", "1,39")
            .Text("TOTALE                             3,39")
            .Text("RIEPILOGO IVA")
            .Text("1   4,00   1,92   0,08")
            .Text("2  10,00   1,26   0,13")
            .Layout();

        var items = ReceiptItemsParser.Parse(layout).Items;

        Assert.Equal(400, items[0].VatRateBasisPoints);
        Assert.Equal(1000, items[1].VatRateBasisPoints);
        Assert.Equal("PRIMOSALE", items[0].RawDescription);
    }

    [Fact]
    public void Codice_non_risolvibile_lascia_la_riga_senza_aliquota()
    {
        var layout = Receipt()
            .Row("PRIMOSALE", "7", "2,00")
            .Row("MORTADELLA", "8", "1,39")
            .Text("TOTALE                             3,39")
            .Layout();

        var items = ReceiptItemsParser.Parse(layout).Items;

        Assert.All(items, i => Assert.Null(i.VatRateBasisPoints));
        Assert.Equal("PRIMOSALE", items[0].RawDescription);
    }

    [Fact]
    public void Aliquota_non_dedotta_quando_la_colonna_manca()
    {
        var layout = Receipt()
            .Row("TOFU BIO", null, "1,49")
            .Text("TOTALE                             1,49")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Null(item.VatRateBasisPoints);
    }

    [Fact]
    public void Riepilogo_iva_non_diventa_una_riga_prodotto()
    {
        var layout = Receipt()
            .Row("PRIMOSALE", "4,00", "2,00")
            .Text("TOTALE                             2,00")
            .Text("RIEPILOGO IVA")
            .Text("4,00   1,92   0,08")
            .Layout();

        var result = ReceiptItemsParser.Parse(layout);

        Assert.Single(result.Items);
        Assert.False(result.VatSummary.IsEmpty);
        Assert.Equal(192, result.VatSummary.TaxableFor(400));
    }

    [Fact]
    public void Descrizione_normalizzata_disponibile_sulla_riga()
    {
        var layout = Receipt()
            .Row("PAST. BARILLA 500", null, "1,29")
            .Text("TOTALE                             1,29")
            .Layout();

        var item = Assert.Single(ReceiptItemsParser.Parse(layout).Items);

        Assert.Equal("past. barilla 500", item.NormalizedDescription);
    }
}
