using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Confronto tra la lettura locale e quella del modello.
/// <para>
/// La regola che questi test proteggono è una sola: <b>si sostituisce solo ciò che non quadrava</b>.
/// Un modello che rimpiazza in silenzio righe corrette è peggio di un modello che non parte.
/// </para>
/// </summary>
public class ReceiptAiComparisonTests
{
    private static ReceiptItemLine Line(long amount) =>
        new("VOCE", "voce", ReceiptItemLine.SingleUnit, ReceiptItemUnit.Piece, null, amount, null,
            amount < 0 ? ReceiptItemKind.Discount : ReceiptItemKind.Product, false, 0);

    /// <summary>Una quadratura con lo scarto voluto rispetto a un totale di 1000.</summary>
    private static ReceiptBalance Balance(long linesTotal, long? receiptTotal = 1000) =>
        ReceiptTotalsCheck.Verify([Line(linesTotal)], receiptTotal);

    [Fact]
    public void Il_modello_quadra_e_il_parser_no_si_propone_il_modello()
    {
        var comparison = ReceiptAiComparison.Compare(local: Balance(700), ai: Balance(1000));

        Assert.Equal(ReceiptReadingChoice.UseAi, comparison.Choice);
    }

    [Fact]
    public void Nessuno_dei_due_quadra_lo_si_dichiara()
    {
        var comparison = ReceiptAiComparison.Compare(local: Balance(700), ai: Balance(800));

        Assert.Equal(ReceiptReadingChoice.NeitherBalances, comparison.Choice);
    }

    [Fact]
    public void Nessuno_dei_due_quadra_si_indica_la_lettura_piu_vicina()
    {
        // Locale a -300 dal totale, modello a -50: il modello è più vicino, e va detto —
        // che è un'informazione, non la promessa che sia giusto.
        var comparison = ReceiptAiComparison.Compare(local: Balance(700), ai: Balance(950));

        Assert.Equal(ReceiptReadingChoice.NeitherBalances, comparison.Choice);
        Assert.True(comparison.AiIsCloser);
    }

    [Fact]
    public void Nessuno_dei_due_quadra_e_il_modello_e_peggio()
    {
        var comparison = ReceiptAiComparison.Compare(local: Balance(950), ai: Balance(400));

        Assert.Equal(ReceiptReadingChoice.NeitherBalances, comparison.Choice);
        Assert.False(comparison.AiIsCloser);
    }

    [Fact]
    public void A_parita_di_scarto_non_si_scomodano_le_righe_locali()
    {
        // Stesso scarto in valore assoluto: il pari non è un miglioramento, e le correzioni
        // già fatte a mano sulle righe locali valgono più di un cambio a somma zero.
        var comparison = ReceiptAiComparison.Compare(local: Balance(900), ai: Balance(1100));

        Assert.Equal(ReceiptReadingChoice.NeitherBalances, comparison.Choice);
        Assert.False(comparison.AiIsCloser);
    }

    [Fact]
    public void Se_le_righe_locali_quadrano_non_si_sostituisce_niente()
    {
        var comparison = ReceiptAiComparison.Compare(local: Balance(1000), ai: Balance(700));

        Assert.Equal(ReceiptReadingChoice.KeepLocal, comparison.Choice);
    }

    [Fact]
    public void Quadrano_entrambe_si_tengono_comunque_le_locali()
    {
        // Caso che in pratica non si presenta — con la quadratura locale riuscita la rilettura
        // non parte nemmeno — ma la regola non deve dipendere da quel presupposto.
        var comparison = ReceiptAiComparison.Compare(local: Balance(1000), ai: Balance(1000));

        Assert.Equal(ReceiptReadingChoice.KeepLocal, comparison.Choice);
    }

    [Fact]
    public void Senza_totale_stampato_non_si_dichiara_migliore_il_modello()
    {
        // Senza totale non c'è termine di paragone: nessuna delle due quadrature è verificabile,
        // e proporre il modello come "più vicino" sarebbe inventare una misura che non esiste.
        var senzaTotale = ReceiptTotalsCheck.Verify([Line(700)], null);

        var comparison = ReceiptAiComparison.Compare(local: Balance(700), ai: senzaTotale);

        Assert.Equal(ReceiptReadingChoice.NeitherBalances, comparison.Choice);
        Assert.False(comparison.AiIsCloser);
    }
}
