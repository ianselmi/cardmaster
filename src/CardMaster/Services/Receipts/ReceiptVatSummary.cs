using System.Text.RegularExpressions;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Una voce del riepilogo IVA: reparto, aliquota, imponibile e imposta.
/// </summary>
/// <param name="Code">Codice di reparto stampato in colonna sulle righe, se presente.</param>
/// <param name="RateBasisPoints">Aliquota in punti base (<c>4,00%</c> → <c>400</c>).</param>
/// <param name="TaxableCents">Imponibile dichiarato, se leggibile.</param>
/// <param name="TaxCents">Imposta dichiarata, se leggibile.</param>
public readonly record struct ReceiptVatEntry(
    string? Code,
    int RateBasisPoints,
    long? TaxableCents,
    long? TaxCents);

/// <summary>
/// Riepilogo IVA a piè di scontrino.
/// <para>
/// Non è una riga prodotto e non deve diventarlo, ma buttarlo via costa due cose: la
/// corrispondenza <b>codice di reparto → aliquota</b> — senza la quale un <c>1</c> stampato in
/// colonna non significa niente — e un termine di confronto per aliquota molto più severo del
/// solo totale, perché due prezzi letti male che si compensano passano la somma complessiva.
/// </para>
/// </summary>
public readonly record struct ReceiptVatSummary(
    IReadOnlyList<ReceiptVatEntry> Entries,
    long? DeclaredTaxCents)
{
    /// <summary>Riepilogo non leggibile: la quadratura per aliquota non si fa.</summary>
    public static ReceiptVatSummary Empty { get; } = new([], null);

    /// <summary>
    /// Vero se non è stato letto nessun riepilogo. Regge anche il <c>default</c> della struct,
    /// che lascia la lista non inizializzata: "nessun riepilogo" è uno stato legittimo e non
    /// deve costringere ogni chiamante a passare <see cref="Empty"/>.
    /// </summary>
    public bool IsEmpty => Entries is not { Count: > 0 };

    /// <summary>Aliquota associata a un codice di reparto, <c>null</c> se il codice non compare.</summary>
    public int? RateForCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || IsEmpty)
        {
            return null;
        }

        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return entry.RateBasisPoints;
            }
        }

        return null;
    }

    /// <summary>Imponibile dichiarato per un'aliquota, <c>null</c> se non leggibile.</summary>
    public long? TaxableFor(int rateBasisPoints)
    {
        if (IsEmpty)
        {
            return null;
        }

        foreach (var entry in Entries)
        {
            if (entry.RateBasisPoints == rateBasisPoints)
            {
                return entry.TaxableCents;
            }
        }

        return null;
    }
}

/// <summary>
/// Legge il riepilogo IVA dalla coda dello scontrino, con regole deterministiche.
/// </summary>
public static class ReceiptVatSummaryParser
{
    /// <summary>
    /// Aliquote IVA italiane, in punti base. L'elenco chiuso è ciò che impedisce di scambiare
    /// un importo qualunque per un'aliquota: <c>10,00</c> in una colonna è ambiguo, <c>10,00</c>
    /// in una colonna di valori tutti appartenenti a questo insieme non lo è più.
    /// </summary>
    public static readonly int[] KnownRates = [0, 400, 500, 1000, 2200];

    /// <summary>Parole che marcano il blocco del riepilogo.</summary>
    private static readonly string[] SummaryKeywords =
    [
        "RIEPILOGO IVA", "RIEPILOGO", "ALIQUOTA", "ALIQ", "IMPONIBILE", "IMPOSTA", "IVA",
    ];

    /// <summary>Codice di reparto in testa alla riga del riepilogo.</summary>
    private static readonly Regex LeadingCodePattern = new(@"^\s*(\d{1,2})\b", RegexOptions.Compiled);

    /// <summary>Aliquota scritta con il segno di percentuale, anche senza decimali.</summary>
    private static readonly Regex PercentPattern = new(
        @"(?<!\d)(\d{1,2})(?:[,.](\d{1,2}))?\s*%",
        RegexOptions.Compiled);

    /// <summary>Legge il riepilogo dalle righe che seguono il corpo dello scontrino.</summary>
    public static ReceiptVatSummary Parse(IReadOnlyList<string> lines, int fromIndex = 0)
    {
        var entries = new List<ReceiptVatEntry>();
        long? declaredTax = null;

        for (var i = Math.Max(0, fromIndex); i < lines.Count; i++)
        {
            var line = lines[i];
            var upper = line.ToUpperInvariant();

            if (declaredTax is null && IsDeclaredTaxLine(upper))
            {
                declaredTax = ReceiptAmount.LastCents(line);
                continue;
            }

            if (!SummaryKeywords.Any(k => upper.Contains(k, StringComparison.Ordinal)) &&
                !LooksLikeSummaryRow(line))
            {
                continue;
            }

            var entry = ParseEntry(line);
            if (entry is not null && !entries.Any(e => e.RateBasisPoints == entry.Value.RateBasisPoints))
            {
                entries.Add(entry.Value);
            }
        }

        return entries.Count == 0 && declaredTax is null
            ? ReceiptVatSummary.Empty
            : new ReceiptVatSummary(entries, declaredTax);
    }

    /// <summary>
    /// Riga che dichiara l'imposta totale ("di cui IVA"). Esclude "IMPONIBILE", che sulla stessa
    /// riga porterebbe a leggere l'imponibile al posto dell'imposta.
    /// </summary>
    private static bool IsDeclaredTaxLine(string upper) =>
        (upper.Contains("DI CUI IVA", StringComparison.Ordinal) ||
         upper.Contains("TOTALE IVA", StringComparison.Ordinal) ||
         upper.Contains("IVA TOTALE", StringComparison.Ordinal)) &&
        !upper.Contains("IMPONIBILE", StringComparison.Ordinal);

    /// <summary>
    /// Riga senza parole chiave ma con la forma di una voce di riepilogo: codice, aliquota nota
    /// e almeno un importo. Serve per i riepiloghi stampati come tabella muta sotto l'intestazione.
    /// </summary>
    private static bool LooksLikeSummaryRow(string line)
    {
        var amounts = ReceiptAmount.Pattern.Matches(line);
        if (amounts.Count < 2)
        {
            return false;
        }

        var first = ReceiptAmount.FirstCents(line);
        return first is not null && KnownRates.Contains((int)first.Value);
    }

    private static ReceiptVatEntry? ParseEntry(string line)
    {
        var rate = FindRate(line);
        if (rate is null)
        {
            return null;
        }

        var amounts = ReceiptAmount.Pattern.Matches(line)
            .Select(m => ReceiptAmount.LastCents(m.Value))
            .Where(c => c is not null)
            .Select(c => c!.Value)
            .ToList();

        // Il primo importo può essere l'aliquota stessa scritta come 4,00: in quel caso non è
        // né imponibile né imposta, e va tolto prima di leggere le due colonne.
        if (amounts.Count > 0 && amounts[0] == rate.Value)
        {
            amounts.RemoveAt(0);
        }

        long? taxable = amounts.Count > 0 ? amounts[0] : null;
        long? tax = amounts.Count > 1 ? amounts[1] : null;

        var code = LeadingCodePattern.Match(line);
        var codeValue = code.Success && code.Groups[1].Value != rate.Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            ? code.Groups[1].Value
            : null;

        return new ReceiptVatEntry(codeValue, rate.Value, taxable, tax);
    }

    /// <summary>
    /// Aliquota della riga: scritta con il <c>%</c>, oppure un importo che appartiene
    /// all'insieme chiuso delle aliquote italiane.
    /// </summary>
    private static int? FindRate(string line)
    {
        var percent = PercentPattern.Match(line);
        if (percent.Success)
        {
            var units = int.Parse(percent.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var decimals = percent.Groups[2].Success
                ? int.Parse(percent.Groups[2].Value.PadRight(2, '0'), System.Globalization.CultureInfo.InvariantCulture)
                : 0;
            var rate = (units * 100) + decimals;
            return KnownRates.Contains(rate) ? rate : null;
        }

        foreach (Match match in ReceiptAmount.Pattern.Matches(line))
        {
            var cents = ReceiptAmount.LastCents(match.Value);
            if (cents is not null && KnownRates.Contains((int)cents.Value) && cents.Value > 0)
            {
                return (int)cents.Value;
            }
        }

        return null;
    }
}
