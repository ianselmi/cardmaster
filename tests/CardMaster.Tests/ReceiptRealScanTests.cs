using System.Globalization;
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
    /// <param name="slope">
    /// Inclinazione costante della foto, come pendenza <c>dy/dx</c>: lo scontrino è dritto ma
    /// fotografato storto.
    /// </param>
    /// <param name="curvature">
    /// Quanto la pendenza <b>cambia</b> dalla cima al fondo: la carta non è appoggiata piana ma
    /// incurvata, e nessuna pendenza unica raddrizza le due estremità insieme. La pendenza locale
    /// va da <c>slope - curvature</c> in cima a <c>slope + curvature</c> in fondo.
    /// </param>
    private static OcrResult Build(double slope = 0, double curvature = 0)
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

        if (slope != 0 || curvature != 0)
        {
            fragments = fragments.Select(f => Warp(f, y, slope, curvature)).ToList();
        }

        var shuffled = fragments.OrderBy(f => f.Bounds.Left).ThenBy(f => f.Bounds.Top).ToList();
        var text = string.Join("\n", shuffled.Select(f => f.Text));
        return new OcrResult(text, [new OcrBlock(text, new Rect(0, 0, 400, y), shuffled)]);
    }

    /// <summary>
    /// Sposta il frammento in verticale come farebbe la prospettiva di una foto: di
    /// <c>pendenza * x</c>, dove la pendenza è quella <b>locale alla sua quota</b>. Con
    /// <paramref name="curvature"/> a zero è l'inclinazione uniforme di una foto storta.
    /// </summary>
    private static OcrLine Warp(OcrLine fragment, double pageHeight, double slope, double curvature)
    {
        var bounds = fragment.Bounds;
        var depth = pageHeight > 0 ? (bounds.Top / pageHeight * 2) - 1 : 0;
        var local = slope + (curvature * depth);
        return new OcrLine(fragment.Text, new Rect(
            bounds.Left,
            bounds.Top + (local * bounds.Center.X),
            bounds.Width,
            bounds.Height));
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

    /// <summary>
    /// Lo stesso scontrino fotografato <b>storto</b>, con un'inclinazione uniforme: la
    /// ricostruzione deve produrre la stessa tabella della foto dritta.
    /// <para>
    /// È la rete che tiene ferma la correzione già esistente — una sola pendenza per tutto lo
    /// scontrino — mentre le si affianca quella a fasce.
    /// </para>
    /// </summary>
    [Fact]
    public void Foto_storta_ricostruisce_lo_stesso_scontrino()
    {
        AssertScontrinoRicostruito(Build(slope: 0.08));
    }

    /// <summary>
    /// Lo scontrino <b>incurvato</b>: la carta non è appoggiata piana, quindi la pendenza cambia
    /// lungo lo scontrino e nessun valore unico raddrizza cima e fondo insieme.
    /// <para>
    /// È il difetto residuo misurato sullo scontrino MD reale (21 righe su 29): la mediana
    /// globale prende la pendenza del centro, e alle estremità l'importo scivola di mezza riga
    /// finendo <b>appaiato al prodotto successivo</b>, che perde così il proprio.
    /// </para>
    /// <para>
    /// I due valori sono <b>il caso più deformato che regge</b>, non una scelta prudente: con la
    /// sola pendenza generale questo stesso scontrino dava 18 righe su 29. Oltre, la
    /// ricostruzione cede — e deve cedere in modo visibile, cioè con la quadratura che fallisce.
    /// </para>
    /// </summary>
    [Fact]
    public void Scontrino_incurvato_ricostruisce_lo_stesso_scontrino()
    {
        AssertScontrinoRicostruito(Build(slope: 0.08, curvature: 0.09));
    }

    /// <summary>
    /// Lo scontrino è ricostruito per intero: tutte le righe, la somma che torna al centesimo con
    /// il totale stampato e con il riepilogo IVA, e <b>nessuna riga che ne inghiotte due</b> —
    /// che è la forma esatta del difetto da deformazione.
    /// </summary>
    private static void AssertScontrinoRicostruito(OcrResult scanned)
    {
        var result = ReceiptItemsParser.Parse(scanned);
        var header = ReceiptHeaderParser.Parse(scanned);

        Assert.Equal(4774, header.TotalCents);
        Assert.Equal(Rows.Length, result.Items.Count);

        var balance = ReceiptTotalsCheck.Verify(result.Items, header.TotalCents, result.VatSummary);
        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.Status);
        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.RateStatus);
        Assert.Equal(0, balance.LinesWithoutRate);

        // Riga per riga, nell'ordine e con il proprio importo: due prodotti finiti nella stessa
        // riga significano un importo perso, ed è così che si perdevano le 8 righe mancanti.
        for (var i = 0; i < Rows.Length; i++)
        {
            var expected = Rows[i];
            var actual = result.Items[i];

            Assert.StartsWith(Prefix(expected.Description), actual.RawDescription, StringComparison.Ordinal);
            Assert.Equal(Cents(expected.Price), actual.AmountCents);
        }
    }

    /// <summary>
    /// Inizio della descrizione, prima del punto in cui il parser stacca quantità e prezzo
    /// unitario (<c>2 X 0,89</c>, <c>0,546 KG X 1,69</c>): quella parte non resta nella
    /// descrizione, e cercarla intera confonderebbe una riga ricostruita bene con una persa.
    /// </summary>
    private static string Prefix(string description) =>
        description[..Math.Min(12, description.Length)];

    private static int Cents(string price) =>
        int.Parse(price.Replace(",", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
}
