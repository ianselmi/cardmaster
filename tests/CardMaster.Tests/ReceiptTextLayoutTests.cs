using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Ricostruzione delle righe visive dalla geometria del riconoscimento.
/// <para>
/// È il pezzo che rende estraibile la testata: ML Kit non restituisce righe ma blocchi,
/// e su uno scontrino a colonne le descrizioni arrivano tutte prima degli importi. Le
/// prove qui sotto riproducono quella forma con rettangoli sintetici, così l'algoritmo
/// è verificabile senza emulatore e senza fotografie.
/// </para>
/// </summary>
public class ReceiptTextLayoutTests
{
    private static OcrLine Line(string text, double x, double y, double width = 200, double height = 30) =>
        new(text, new Rect(x, y, width, height));

    private static OcrBlock Block(params OcrLine[] lines) =>
        new(string.Empty, new Rect(0, 0, 0, 0), lines);

    /// <summary>
    /// Uno scontrino come lo restituisce davvero il riconoscimento: un blocco con le
    /// descrizioni, un altro con gli importi alle stesse quote verticali.
    /// </summary>
    private static OcrResult ScontrinoAColonne() => new(
        "testo grezzo con le colonne separate",
        [
            Block(
                Line("ESSELUNGA S.P.A.", 40, 20, 320),
                Line("P.IVA 04916380159", 40, 60, 320)),
            Block(
                Line("SUBTOTALE", 40, 100),
                Line("SCONTO FIDATY", 40, 140),
                Line("TOTALE COMPLESSIVO", 40, 180)),
            Block(
                Line("7,11", 500, 102, 90),
                Line("-0, 50", 500, 142, 90),
                Line("6,61", 500, 182, 90)),
            Block(Line("11/08/ 2026 18:42", 40, 240, 320)),
        ]);

    [Fact]
    public void Le_colonne_tornano_sulla_stessa_riga()
    {
        var lines = ReceiptTextLayout.ToVisualLines(ScontrinoAColonne());

        Assert.Equal(
            [
                "ESSELUNGA S.P.A.",
                "P.IVA 04916380159",
                "SUBTOTALE  7,11",
                "SCONTO FIDATY  -0, 50",
                "TOTALE COMPLESSIVO  6,61",
                "11/08/ 2026 18:42",
            ],
            lines);
    }

    [Fact]
    public void La_testata_si_estrae_dalla_geometria_non_dal_testo_grezzo()
    {
        // Sul testo grezzo di questo stesso scontrino il totale NON è estraibile:
        // "TOTALE COMPLESSIVO" e "6,61" stanno in blocchi diversi.
        var header = ReceiptHeaderParser.Parse(
            ScontrinoAColonne(),
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.FromHours(2)));

        Assert.Equal("ESSELUNGA S.P.A.", header.MerchantName);
        Assert.Equal("04916380159", header.MerchantVatId);
        Assert.Equal(661, header.TotalCents);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 11, 18, 42, 0, TimeSpan.FromHours(2)),
            header.PurchasedAt);
    }

    [Fact]
    public void I_frammenti_di_una_riga_si_ordinano_da_sinistra_a_destra()
    {
        var result = new OcrResult("", [Block(
            Line("6,61", 500, 100, 90),
            Line("TOTALE", 40, 100))]);

        Assert.Equal(["TOTALE  6,61"], ReceiptTextLayout.ToVisualLines(result));
    }

    [Fact]
    public void Righe_vicine_ma_distinte_non_vengono_fuse()
    {
        // Bande verticali che non si sovrappongono: due righe, non una.
        var result = new OcrResult("", [Block(
            Line("PANE INTEGRALE", 40, 100, 200, 30),
            Line("LATTE PS 1L", 40, 140, 200, 30))]);

        Assert.Equal(["PANE INTEGRALE", "LATTE PS 1L"], ReceiptTextLayout.ToVisualLines(result));
    }

    [Fact]
    public void Le_righe_escono_ordinate_dall_alto_verso_il_basso()
    {
        // I blocchi arrivano in ordine arbitrario: conta la posizione, non l'ordine di arrivo.
        var result = new OcrResult("", [
            Block(Line("IN FONDO", 40, 300)),
            Block(Line("IN CIMA", 40, 20)),
            Block(Line("IN MEZZO", 40, 160)),
        ]);

        Assert.Equal(["IN CIMA", "IN MEZZO", "IN FONDO"], ReceiptTextLayout.ToVisualLines(result));
    }

    [Fact]
    public void Senza_geometria_utilizzabile_si_ripiega_sul_testo_grezzo()
    {
        // Meglio il testo grezzo che nessun testo: l'utente deve poter correggere a mano.
        var result = new OcrResult("PRIMA RIGA\nSECONDA RIGA", []);

        Assert.Equal(["PRIMA RIGA", "SECONDA RIGA"], ReceiptTextLayout.ToVisualLines(result));
    }

    [Fact]
    public void Esito_vuoto_non_produce_righe()
    {
        Assert.Empty(ReceiptTextLayout.ToVisualLines(OcrResult.Empty));
    }

    /// <summary>
    /// Scontrino fotografato storto: ogni frammento è un po' più in basso del precedente, e
    /// ognuno si sovrappone al successivo quanto basta. Senza un limite all'altezza della riga
    /// la sovrapposizione si propaga per transitività e le righe collassano in una sola.
    /// <para>
    /// Non è un caso teorico: su uno scontrino MD reale sette prodotti sono finiti su una riga
    /// con tutte le loro aliquote e tutti i loro prezzi in coda, rendendoli irrecuperabili.
    /// </para>
    /// </summary>
    [Fact]
    public void Foto_storta_non_fa_collassare_le_righe_una_sull_altra()
    {
        // Passo verticale 18 su altezza 30: ogni coppia adiacente si sovrappone del 40%, ma
        // dalla prima all'ultima ci sono tre righe di distanza.
        var result = new OcrResult("", [
            Block(
                Line("PRIMO PRODOTTO", 40, 0),
                Line("SECONDO PRODOTTO", 40, 18),
                Line("TERZO PRODOTTO", 40, 36),
                Line("QUARTO PRODOTTO", 40, 54)),
        ]);

        var lines = ReceiptTextLayout.ToVisualLines(result);

        Assert.True(lines.Count >= 3, $"Righe collassate: {lines.Count} invece di almeno 3.");
        Assert.DoesNotContain(lines, l => l.Contains("PRIMO", StringComparison.Ordinal) &&
                                          l.Contains("QUARTO", StringComparison.Ordinal));
    }
}
