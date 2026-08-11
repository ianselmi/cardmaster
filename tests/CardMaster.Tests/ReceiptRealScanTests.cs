using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Uno scontrino reale dall'inizio alla fine: 29 righe, due sconti, tre aliquote, prodotti a
/// pezzi e a peso.
/// <para>
/// I casi singoli provano una regola per volta; questo prova che le regole <b>convivono</b>. È
/// il test che dice se la ricostruzione vale la pena: se la somma delle righe lette torna al
/// centesimo con il totale stampato e con il riepilogo IVA, allora quello che l'app mostra
/// all'utente è lo scontrino, non una sua approssimazione.
/// </para>
/// </summary>
public class ReceiptRealScanTests
{
    private const double DescriptionX = 10;
    private const double VatX = 250;
    private const double PriceX = 330;

    /// <summary>Righe dello scontrino: descrizione, aliquota in colonna, importo.</summary>
    private static readonly (string Description, string Vat, string Price)[] Rows =
    [
        ("PRIMOSALE S/LATTOSIO V M", "4,00", "2,00"),
        ("MORTADELLA BOLOGNA IGP", "10,00", "1,39"),
        ("PROSC.COTTO ALTA QUALITA", "10,00", "1,00"),
        ("POLPETTE VEGETARIANE", "10,00", "2,29"),
        ("BURGER VEG. MELANZANE", "10,00", "1,79"),
        ("AFFET. VEGETALE G.COTTO", "10,00", "1,49"),
        ("MINIBURGER VEG CAROTE/CECI", "10,00", "1,79"),
        ("MOZZARELLA S/LATT 2 X 0,89", "4,00", "1,78"),
        ("TOFU BIO", "10,00", "1,49"),
        ("SPRAY VETRI ECOLOGICO", "22,00", "1,29"),
        ("MINI BITES THAI STYLE", "10,00", "1,99"),
        ("SCONTO 50% MINI BITES", "10,00", "-1,00"),
        ("YOGURT SKYR BIANCO 2 X 0,59", "4,00", "1,18"),
        ("2 RICOTTE S/LATTOSIO", "4,00", "1,09"),
        ("PHILADELPHIA LIGHT TWIN", "4,00", "1,69"),
        ("6 BARRETTE MIRTILLI", "10,00", "1,00"),
        ("6 BARRETTE CEREALI CLASS.", "10,00", "1,00"),
        ("DEMI BAGUETTE MULTICEREALI", "4,00", "0,61"),
        ("SCONTO 13,94% OFF. VOLANTINO", "4,00", "-0,08"),
        ("SNACK LEGUMI", "10,00", "0,99"),
        ("COUS COUS CON VERDURE", "10,00", "1,79"),
        ("50 CAPS BORBONE A MODO MIO", "22,00", "12,49"),
        ("15 SACCH. MATER-BI 50X60", "22,00", "1,39"),
        ("ZUCCHINE SCURE 0,546 KG X 1,69", "4,00", "0,92"),
        ("ROTOLO SHOPPER ORTOFRUTTA", "22,00", "0,01"),
        ("CAROTE KG 1", "4,00", "1,19"),
        ("PESCA GIALLA KG 1", "4,00", "1,99"),
        ("50 BICCHIERI CAFFE 70CC", "22,00", "1,49"),
        ("SGRASSATORE OXY MOUSSE", "22,00", "1,69"),
    ];

    /// <summary>
    /// Costruisce lo scontrino con i frammenti <b>rimescolati per colonna</b>, come li
    /// restituisce ML Kit: prima tutte le descrizioni, poi tutte le aliquote, poi tutti i prezzi.
    /// </summary>
    private static OcrResult Build()
    {
        var fragments = new List<OcrLine>();
        double y = 0;

        void Add(string text, double x, double width)
            => fragments.Add(new OcrLine(text, new Rect(x, y, width, 18)));

        void Text(string text)
        {
            Add(text, DescriptionX, 220);
            y += 30;
        }

        Text("MD SPA");
        Text("VIA DELLE ACACIE 12 - 00100 ROMA");
        Text("P.IVA 12345678901");
        Text("DOCUMENTO COMMERCIALE");

        foreach (var row in Rows)
        {
            Add(row.Description, DescriptionX, 220);
            Add(row.Vat, VatX, 45);
            Add(row.Price, PriceX, 55);
            y += 30;
        }

        Text("TOTALE COMPLESSIVO                47,74");
        Text("DI CUI IVA                         5,34");
        Text("PAGAMENTO BANCOMAT                47,74");
        Text("RIEPILOGO IVA");
        Text("4,00    11,89    0,48");
        Text("10,00   15,46    1,55");
        Text("22,00   15,05    3,31");

        var shuffled = fragments.OrderBy(f => f.Bounds.Left).ThenBy(f => f.Bounds.Top).ToList();
        var text = string.Join("\n", shuffled.Select(f => f.Text));
        return new OcrResult(text, [new OcrBlock(text, new Rect(0, 0, 400, y), shuffled)]);
    }

    [Fact]
    public void Ricostruisce_tutte_le_righe_dello_scontrino()
    {
        var result = ReceiptItemsParser.Parse(Build());

        Assert.True(result.BodyFound);
        Assert.Equal(Rows.Length, result.Items.Count);
    }

    [Fact]
    public void La_somma_delle_righe_lette_torna_con_il_totale_stampato()
    {
        var result = ReceiptItemsParser.Parse(Build());
        var header = ReceiptHeaderParser.Parse(Build());

        var balance = ReceiptTotalsCheck.Verify(result.Items, header.TotalCents, result.VatSummary);

        Assert.Equal(4774, header.TotalCents);
        Assert.Equal(534, header.TaxCents);
        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.Status);
    }

    [Fact]
    public void Quadra_anche_per_aliquota()
    {
        var result = ReceiptItemsParser.Parse(Build());
        var balance = ReceiptTotalsCheck.Verify(result.Items, 4774, result.VatSummary);

        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.RateStatus);
        Assert.Equal(3, balance.Rates.Count);
        Assert.Equal(0, balance.LinesWithoutRate);
    }

    [Fact]
    public void Gli_sconti_sono_sconti_e_non_prodotti()
    {
        var items = ReceiptItemsParser.Parse(Build()).Items;

        var discounts = items.Where(i => i.Kind == ReceiptItemKind.Discount).ToList();

        Assert.Equal(2, discounts.Count);
        Assert.All(discounts, d => Assert.True(d.AmountCents < 0));
    }

    [Fact]
    public void Quantita_e_peso_letti_dove_lo_scontrino_li_stampa()
    {
        var items = ReceiptItemsParser.Parse(Build()).Items;

        var mozzarella = items.Single(i => i.RawDescription.StartsWith("MOZZARELLA", StringComparison.Ordinal));
        Assert.Equal(2000, mozzarella.QuantityMilli);
        Assert.Equal(89, mozzarella.UnitPriceCents);
        Assert.Equal(ReceiptItemUnit.Piece, mozzarella.Unit);

        var zucchine = items.Single(i => i.RawDescription.StartsWith("ZUCCHINE", StringComparison.Ordinal));
        Assert.Equal(546, zucchine.QuantityMilli);
        Assert.Equal(169, zucchine.UnitPriceCents);
        Assert.Equal(ReceiptItemUnit.Kilogram, zucchine.Unit);
        Assert.Equal(92, zucchine.AmountCents);
    }

    [Fact]
    public void Nessuna_riga_incoerente()
    {
        var items = ReceiptItemsParser.Parse(Build()).Items;

        Assert.All(items, i => Assert.False(i.IsInconsistent));
    }
}
