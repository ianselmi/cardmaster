namespace CardMaster.Services.Receipts;

/// <summary>Esito di un confronto tra ciò che è stato letto e ciò che lo scontrino dichiara.</summary>
public enum ReceiptBalanceStatus
{
    /// <summary>Confronto non eseguito: manca il termine di paragone.</summary>
    NotChecked = 0,

    /// <summary>Le somme coincidono al centesimo.</summary>
    Balanced = 1,

    /// <summary>Le somme non coincidono: lo scarto è dichiarato.</summary>
    Mismatch = 2,
}

/// <summary>Confronto per una singola aliquota.</summary>
/// <param name="RateBasisPoints">Aliquota in punti base.</param>
/// <param name="LinesCents">Somma delle righe che portano questa aliquota.</param>
/// <param name="DeclaredGrossCents">
/// Importo <b>lordo</b> dichiarato dal riepilogo per questa aliquota, cioè imponibile più
/// imposta. Il confronto va fatto sul lordo perché i prezzi di riga di uno scontrino italiano
/// sono IVA inclusa: confrontarli con il solo imponibile farebbe fallire ogni quadratura di un
/// importo pari all'imposta, e la verifica non troverebbe più nulla perché segnalerebbe tutto.
/// </param>
public readonly record struct ReceiptRateBalance(
    int RateBasisPoints,
    long LinesCents,
    long DeclaredGrossCents)
{
    /// <summary>Scarto in centesimi: positivo se le righe superano il dichiarato.</summary>
    public long DifferenceCents => LinesCents - DeclaredGrossCents;

    /// <summary>Vero se questa aliquota quadra.</summary>
    public bool IsBalanced => DifferenceCents == 0;
}

/// <summary>
/// Esito della quadratura di uno scontrino.
/// </summary>
/// <param name="Status">Esito del confronto con il totale di testata.</param>
/// <param name="LinesTotalCents">Somma delle righe, sconti compresi.</param>
/// <param name="ReceiptTotalCents">Totale di testata, se noto.</param>
/// <param name="RateStatus">Esito del confronto per aliquota.</param>
/// <param name="Rates">Dettaglio per aliquota, vuoto se il riepilogo non è leggibile.</param>
/// <param name="LinesWithoutRate">Quante righe sono rimaste senza aliquota.</param>
public readonly record struct ReceiptBalance(
    ReceiptBalanceStatus Status,
    long LinesTotalCents,
    long? ReceiptTotalCents,
    ReceiptBalanceStatus RateStatus,
    IReadOnlyList<ReceiptRateBalance> Rates,
    int LinesWithoutRate)
{
    /// <summary>Scarto rispetto al totale, positivo se le righe superano il totale.</summary>
    public long DifferenceCents => LinesTotalCents - (ReceiptTotalCents ?? LinesTotalCents);

    /// <summary>Aliquote che non tornano.</summary>
    public IEnumerable<ReceiptRateBalance> UnbalancedRates => Rates.Where(r => !r.IsBalanced);
}

/// <summary>
/// Confronta la somma delle righe con quello che lo scontrino dichiara.
/// <para>
/// La tolleranza è <b>zero centesimi</b>, e non per severità fine a sé stessa: un margine
/// nasconderebbe proprio l'errore che questa verifica esiste per trovare. Un prezzo letto con
/// una cifra di troppo produce uno scarto grande e vistoso; uno letto con la virgola spostata
/// ne produce uno piccolo, ed è il secondo quello pericoloso.
/// </para>
/// <para>
/// Nessuna correzione: non si aggiunge una riga "differenza", non si aggiusta l'ultimo prezzo,
/// non si rifiuta il salvataggio. La discrepanza è un segnale per l'utente, non un errore da
/// nascondere.
/// </para>
/// </summary>
public static class ReceiptTotalsCheck
{
    /// <summary>Verifica righe contro totale di testata e, se leggibile, contro il riepilogo IVA.</summary>
    public static ReceiptBalance Verify(
        IReadOnlyList<ReceiptItemLine> items,
        long? receiptTotalCents,
        ReceiptVatSummary vatSummary = default)
    {
        var linesTotal = items.Sum(i => i.AmountCents);

        var status = receiptTotalCents is null
            ? ReceiptBalanceStatus.NotChecked
            : linesTotal == receiptTotalCents.Value
                ? ReceiptBalanceStatus.Balanced
                : ReceiptBalanceStatus.Mismatch;

        var withoutRate = items.Count(i => i.VatRateBasisPoints is null);
        var rates = BuildRates(items, vatSummary);

        var rateStatus = rates.Count == 0
            ? ReceiptBalanceStatus.NotChecked
            : rates.All(r => r.IsBalanced)
                ? ReceiptBalanceStatus.Balanced
                : ReceiptBalanceStatus.Mismatch;

        return new ReceiptBalance(status, linesTotal, receiptTotalCents, rateStatus, rates, withoutRate);
    }

    /// <summary>
    /// Confronto per aliquota: somma delle righe di ciascuna aliquota contro l'imponibile
    /// dichiarato. È il controllo che trova gli errori che si compensano, invisibili al totale.
    /// <para>
    /// Le righe rimaste senza aliquota non vengono attribuite a nessuna per far tornare i conti:
    /// restano fuori e contate a parte, perché attribuirle sarebbe inventare il dato che manca.
    /// </para>
    /// </summary>
    private static List<ReceiptRateBalance> BuildRates(
        IReadOnlyList<ReceiptItemLine> items,
        ReceiptVatSummary vatSummary)
    {
        var rates = new List<ReceiptRateBalance>();
        if (vatSummary.IsEmpty)
        {
            return rates;
        }

        foreach (var entry in vatSummary.Entries)
        {
            var gross = GrossOf(entry);
            if (gross is null)
            {
                continue;
            }

            var sum = items
                .Where(i => i.VatRateBasisPoints == entry.RateBasisPoints)
                .Sum(i => i.AmountCents);

            rates.Add(new ReceiptRateBalance(entry.RateBasisPoints, sum, gross.Value));
        }

        return rates;
    }

    /// <summary>
    /// Lordo dichiarato per una voce del riepilogo. Quando lo scontrino stampa anche l'imposta
    /// si somma quella, senza calcolare niente; solo se manca si ricava dall'aliquota, ed è
    /// l'unico punto in cui un arrotondamento entra nel confronto.
    /// </summary>
    private static long? GrossOf(ReceiptVatEntry entry)
    {
        if (entry.TaxableCents is null)
        {
            return null;
        }

        if (entry.TaxCents is not null)
        {
            return entry.TaxableCents.Value + entry.TaxCents.Value;
        }

        return (long)Math.Round(
            entry.TaxableCents.Value * (10000m + entry.RateBasisPoints) / 10000m,
            MidpointRounding.AwayFromZero);
    }
}
