using System.Globalization;
using System.Text.RegularExpressions;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Dati di testata estratti dal testo di uno scontrino. Ogni campo è <c>null</c> quando
/// <b>non è stato riconosciuto</b>: il parser non inventa e non deduce valori assenti, perché
/// è questo che rende credibile la schermata di correzione (l'utente deve sapere cosa è stato
/// letto e cosa no).
/// </summary>
public readonly record struct ReceiptHeader(
    string? MerchantName,
    string? MerchantVatId,
    DateTimeOffset? PurchasedAt,
    long? TotalCents)
{
    public static ReceiptHeader Empty { get; }
}

/// <summary>
/// Estrae esercente, partita IVA, data e totale dal testo riconosciuto di uno scontrino,
/// con regole deterministiche.
/// <para>
/// La testata è la parte <b>regolare</b> dello scontrino, quella dove le regole vincono: qui
/// non serve un modello. L'irregolarità sta nelle descrizioni dei prodotti, ed è per questo
/// che vive in un'altra change.
/// </para>
/// <para>
/// Classe pura: nessuna dipendenza da MAUI, da ML Kit o dal database. Prende testo e
/// restituisce campi, così è verificabile su testo OCR reale senza emulatore.
/// </para>
/// </summary>
public static class ReceiptHeaderParser
{
    /// <summary>Quanto indietro nel tempo una data resta plausibile per uno scontrino.</summary>
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(365 * 5);

    /// <summary>Tolleranza sul futuro: copre fusi orari e orologio del device leggermente avanti.</summary>
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromDays(1);

    /// <summary>
    /// Importo in formato italiano: virgola decimale obbligatoria, punto opzionale come
    /// separatore delle migliaia. Es. <c>1.234,56</c>, <c>7,90</c>.
    /// </summary>
    private static readonly Regex AmountPattern = new(
        @"(?<!\d)(\d{1,3}(?:\.\d{3})+|\d+),\s?(\d{2})(?!\d)",
        RegexOptions.Compiled);

    /// <summary>
    /// Data con separatori <c>/ - .</c> e spazi tollerati attorno ad essi: l'OCR spezza
    /// volentieri i gruppi (visto su emulatore: <c>11/08/ 2026</c>), e senza questa tolleranza
    /// la data non viene riconosciuta pur essendo perfettamente leggibile.
    /// </summary>
    private static readonly Regex DatePattern = new(
        @"(?<!\d)(\d{1,2})\s*[\/\-.]\s*(\d{1,2})\s*[\/\-.]\s*(\d{4}|\d{2})(?!\d)",
        RegexOptions.Compiled);

    /// <summary>
    /// Ora nel formato <c>hh:mm</c>. <b>Solo i due punti</b>: accettare anche il punto faceva
    /// agganciare al parser i primi due gruppi di una data puntata (<c>05.08.2026</c> letto come
    /// le 05:08). Meglio perdere l'ora su qualche scontrino che registrarne una sbagliata.
    /// </summary>
    private static readonly Regex TimePattern = new(
        @"(?<!\d)([01]?\d|2[0-3]):([0-5]\d)(?!\d)",
        RegexOptions.Compiled);

    private static readonly Regex VatPattern = new(@"(?<!\d)(\d{11})(?!\d)", RegexOptions.Compiled);

    /// <summary>Parole che marcano la riga del totale, dalla più specifica alla più generica.</summary>
    private static readonly string[] TotalKeywords =
    [
        "TOTALE COMPLESSIVO",
        "TOTALE EURO",
        "TOT. EURO",
        "TOT EURO",
        "IMPORTO PAGATO",
        "TOTALE",
    ];

    /// <summary>
    /// Righe che contengono una parola di totale ma <b>non sono</b> il totale dello scontrino.
    /// Senza questa esclusione "SUBTOTALE" e "TOTALE SCONTI" vincerebbero sul totale vero.
    /// </summary>
    private static readonly string[] TotalExclusions =
    [
        "SUBTOTALE",
        "SUB TOTALE",
        "SCONT",
        "RESTO",
        "NON RISCOSSO",
        "PARZIALE",
    ];

    /// <summary>Righe di intestazione che non sono il nome dell'esercente.</summary>
    private static readonly string[] MerchantExclusions =
    [
        "DOCUMENTO COMMERCIALE",
        "SCONTRINO",
        "RICEVUTA",
        "P.IVA",
        "PARTITA IVA",
        "COD. FISC",
        "CODICE FISCALE",
        "TEL.",
        "TELEFONO",
        "WWW.",
    ];

    /// <summary>Prefissi di indirizzo: utili a riconoscere una riga che non è l'insegna.</summary>
    private static readonly string[] AddressPrefixes =
    [
        "VIA ", "V.LE", "VIALE", "P.ZA", "PIAZZA", "CORSO", "C.SO", "LARGO", "LOC.", "STRADA",
    ];

    /// <summary>
    /// Estrae i dati di testata dall'esito dell'OCR, <b>ricostruendo prima le righe visive</b>
    /// dalla geometria. È la via da usare per uno scontrino appena acquisito: sul testo grezzo
    /// di ML Kit descrizioni e importi finiscono in blocchi separati, e la riga del totale
    /// arriva senza il suo importo.
    /// </summary>
    public static ReceiptHeader Parse(OcrResult result, DateTimeOffset? now = null) =>
        Parse(ReceiptTextLayout.ToVisualText(result), now);

    /// <summary>
    /// Estrae i dati di testata da testo già in righe. Restituisce
    /// <see cref="ReceiptHeader.Empty"/> per testo vuoto.
    /// </summary>
    /// <param name="rawText">Testo riconosciuto integrale.</param>
    /// <param name="now">Istante di riferimento per la plausibilità della data (default: adesso).</param>
    public static ReceiptHeader Parse(string? rawText, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return ReceiptHeader.Empty;
        }

        var lines = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        return new ReceiptHeader(
            FindMerchantName(lines),
            FindVatId(lines),
            FindPurchasedAt(lines, now ?? DateTimeOffset.Now),
            FindTotalCents(lines));
    }

    /// <summary>
    /// Cerca il totale scorrendo le parole chiave dalla più specifica alla più generica; a
    /// parità di parola vince l'occorrenza <b>più in basso</b>, perché il totale sta in fondo.
    /// Se la riga con la parola chiave non contiene importi, guarda la riga successiva: molti
    /// scontrini mandano a capo la cifra.
    /// </summary>
    private static long? FindTotalCents(List<string> lines)
    {
        foreach (var keyword in TotalKeywords)
        {
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                var upper = lines[i].ToUpperInvariant();
                if (!upper.Contains(keyword, StringComparison.Ordinal))
                {
                    continue;
                }

                if (TotalExclusions.Any(x => upper.Contains(x, StringComparison.Ordinal)))
                {
                    continue;
                }

                var amount = LastAmountCents(lines[i]);
                if (amount is not null)
                {
                    return amount;
                }

                if (i + 1 < lines.Count)
                {
                    var next = LastAmountCents(lines[i + 1]);
                    if (next is not null)
                    {
                        return next;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>Ultimo importo della riga (il prezzo sta a destra), in centesimi.</summary>
    private static long? LastAmountCents(string line)
    {
        var matches = AmountPattern.Matches(line);
        if (matches.Count == 0)
        {
            return null;
        }

        var match = matches[^1];
        var whole = match.Groups[1].Value.Replace(".", string.Empty, StringComparison.Ordinal);
        var cents = match.Groups[2].Value;

        if (!long.TryParse(whole, NumberStyles.None, CultureInfo.InvariantCulture, out var units) ||
            !long.TryParse(cents, NumberStyles.None, CultureInfo.InvariantCulture, out var fraction))
        {
            return null;
        }

        return (units * 100) + fraction;
    }

    /// <summary>
    /// Prima data plausibile, con l'ora della stessa riga se presente. Una data sbagliata è
    /// peggio di una mancante: falsa i totali mensili in silenzio, mentre un campo vuoto si vede.
    /// </summary>
    private static DateTimeOffset? FindPurchasedAt(List<string> lines, DateTimeOffset now)
    {
        foreach (var line in lines)
        {
            var match = DatePattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            if (!int.TryParse(match.Groups[1].Value, out var day) ||
                !int.TryParse(match.Groups[2].Value, out var month) ||
                !int.TryParse(match.Groups[3].Value, out var year))
            {
                continue;
            }

            if (match.Groups[3].Value.Length == 2)
            {
                year += 2000;
            }

            if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                continue;
            }

            var hour = 0;
            var minute = 0;
            var time = TimePattern.Match(line);
            if (time.Success)
            {
                _ = int.TryParse(time.Groups[1].Value, out hour);
                _ = int.TryParse(time.Groups[2].Value, out minute);
            }

            var candidate = new DateTimeOffset(
                new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified),
                now.Offset);

            if (candidate > now + FutureTolerance || candidate < now - MaxAge)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Partita IVA: 11 cifre. Vince quella su una riga che la dichiara ("P.IVA", "PARTITA IVA"),
    /// altrimenti la prima sequenza di 11 cifre incontrata.
    /// </summary>
    private static string? FindVatId(List<string> lines)
    {
        foreach (var line in lines)
        {
            var upper = line.ToUpperInvariant();
            if (!upper.Contains("P.IVA", StringComparison.Ordinal) &&
                !upper.Contains("PIVA", StringComparison.Ordinal) &&
                !upper.Contains("PARTITA IVA", StringComparison.Ordinal))
            {
                continue;
            }

            var declared = VatPattern.Match(line);
            if (declared.Success)
            {
                return declared.Groups[1].Value;
            }
        }

        foreach (var line in lines)
        {
            var any = VatPattern.Match(line);
            if (any.Success)
            {
                return any.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Insegna: prima riga in cima allo scontrino che sembri un nome — abbastanza lettere,
    /// non un indirizzo, non un'intestazione fiscale, non solo numeri.
    /// </summary>
    private static string? FindMerchantName(List<string> lines)
    {
        foreach (var line in lines.Take(8))
        {
            var upper = line.ToUpperInvariant();

            if (line.Length < 3 ||
                MerchantExclusions.Any(x => upper.Contains(x, StringComparison.Ordinal)) ||
                AddressPrefixes.Any(p => upper.StartsWith(p, StringComparison.Ordinal)))
            {
                continue;
            }

            var letters = line.Count(char.IsLetter);
            if (letters < 3 || letters < line.Length / 2)
            {
                continue;
            }

            return line;
        }

        return null;
    }
}
