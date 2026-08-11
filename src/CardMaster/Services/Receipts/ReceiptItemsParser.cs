using System.Globalization;
using System.Text.RegularExpressions;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Esito della ricostruzione delle righe prodotto.
/// </summary>
/// <param name="Items">Righe ricostruite, nell'ordine dello scontrino.</param>
/// <param name="VatSummary">Riepilogo IVA letto dalla coda, vuoto se non leggibile.</param>
/// <param name="BodyFound">
/// Falso quando il corpo non è delimitabile — tipicamente perché manca la riga del totale. In
/// quel caso non si propone niente: estendere il corpo fino in fondo produrrebbe prodotti
/// fatti di resto, punti fedeltà e riepiloghi.
/// </param>
public readonly record struct ReceiptItemsResult(
    IReadOnlyList<ReceiptItemLine> Items,
    ReceiptVatSummary VatSummary,
    bool BodyFound)
{
    /// <summary>Nessuna riga ricostruita.</summary>
    public static ReceiptItemsResult None { get; } = new([], ReceiptVatSummary.Empty, false);
}

/// <summary>
/// Ricostruisce le righe prodotto dalla <b>geometria</b> dell'OCR.
/// <para>
/// Il punto di tutta la classe è che descrizione e prezzo si separano per <b>posizione</b>, non
/// per ordine del testo: in <c>PROSCIUTTO 100 GR   4,50</c> l'ultimo numero della riga è il
/// prezzo, ma in <c>PASTA 500 GR</c> l'ultimo numero è parte del nome. La differenza è dove
/// cade il numero rispetto alla colonna dei prezzi, e la colonna si stabilisce guardando lo
/// scontrino intero, non la singola riga.
/// </para>
/// <para>
/// Classe pura: niente MAUI, niente ML Kit, niente database. Le regole hanno un nome ciascuna
/// e un test ciascuna, perché è qui che un errore non fa rumore — una riga letta 15,00 invece
/// di 1,50 sparisce dentro un totale che a occhio quadra.
/// </para>
/// </summary>
public static class ReceiptItemsParser
{
    /// <summary>
    /// Quanto due frammenti possono distare in orizzontale restando nella stessa colonna, in
    /// frazione della larghezza dello scontrino. Le colonne di un registratore di cassa sono
    /// allineate al carattere: serve tolleranza per il rumore dell'OCR, non per le colonne larghe.
    /// </summary>
    private const double ColumnTolerance = 0.06;

    /// <summary>
    /// Quantità per prezzo unitario: <c>2 X 1,50</c>, <c>2 PZ x 1,50</c>, <c>0,432 kg x 2,99</c>.
    /// </summary>
    private static readonly Regex QuantityPattern = new(
        @"(?<!\d)(?<qty>\d{1,3}(?:[,.]\d{1,3})?)\s*(?<unit>KG|GR|G|LT|L|PZ|PZ\.|NR|N)?\s*[X×*]\s*(?<price>\d{1,4}[,.]\d{2})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Unità di peso al denominatore del prezzo: <c>2,99 €/kg</c>.</summary>
    private static readonly Regex PerKiloPattern = new(
        @"[€]?\s*/\s*(KG|G|GR)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Frammento che è solo un codice di reparto: una o due cifre.</summary>
    private static readonly Regex CodePattern = new(@"^\s*(\d{1,2})\s*$", RegexOptions.Compiled);

    /// <summary>Marcatori di sconto: la riga vale come importo negativo, non come prodotto.</summary>
    private static readonly string[] DiscountKeywords =
    [
        "SCONTO", "SCONTI", "PROMO", "OFFERTA", "RIDUZIONE", "BUONO", "OMAGGIO",
    ];

    /// <summary>
    /// Righe di servizio: non sono prodotti nemmeno quando hanno un importo in colonna.
    /// </summary>
    /// <remarks>
    /// Solo parole che <b>non possono</b> essere un prodotto. "CARTA" e "CONTANTE" sembrano
    /// candidate ovvie e sono escluse apposta: la prima è carta igienica, la seconda compare
    /// dopo il totale, cioè fuori dal corpo. Una parola di troppo qui fa sparire un acquisto
    /// senza lasciare traccia.
    /// </remarks>
    private static readonly string[] ServiceKeywords =
    [
        "REPARTO", "PEZZI N", "N. PEZZI", "ARTICOLI", "IMPONIBILE", "IMPOSTA", "ALIQUOTA",
        "RIEPILOGO", "SUBTOTALE", "SUB TOTALE", "TOTALE", "RESTO", "ARROTONDAMENTO",
        "NON RISCOSSO", "SCONTRINO", "DOCUMENTO COMMERCIALE",
    ];

    /// <summary>Ricostruisce le righe dall'esito dell'OCR.</summary>
    public static ReceiptItemsResult Parse(OcrResult result) =>
        Parse(ReceiptTextLayout.ToVisualLayout(result));

    /// <summary>Ricostruisce le righe dalle righe visive con la loro geometria.</summary>
    public static ReceiptItemsResult Parse(IReadOnlyList<ReceiptVisualLine> layout)
    {
        if (layout.Count == 0)
        {
            return ReceiptItemsResult.None;
        }

        var texts = layout.Select(l => l.Text).ToList();

        var bodyEnd = FindBodyEnd(texts);
        if (bodyEnd < 0)
        {
            // Senza la riga del totale il corpo non ha un confine inferiore: non si indovina.
            return new ReceiptItemsResult([], ReceiptVatSummary.Empty, false);
        }

        // Il riepilogo si cerca <b>solo nella coda</b>: una riga prodotto con l'aliquota in
        // colonna ha la stessa forma di una voce di riepilogo (una percentuale nota e un
        // importo), e cercandolo su tutto lo scontrino il primo prodotto vincerebbe sul
        // riepilogo vero, falsando ogni confronto per aliquota.
        var summary = ReceiptVatSummaryParser.Parse(texts, bodyEnd);

        var body = layout.Take(bodyEnd).Where(l => l.HasGeometry).ToList();
        if (body.Count == 0)
        {
            return new ReceiptItemsResult([], summary, false);
        }

        var columns = ResolveColumns(body);
        if (columns is null)
        {
            return new ReceiptItemsResult([], summary, false);
        }

        return new ReceiptItemsResult(BuildItems(body, columns.Value, summary), summary, true);
    }

    /// <summary>
    /// Fine del corpo: la riga del totale, o il primo <c>SUBTOTALE</c> che la precede.
    /// <c>-1</c> quando il totale non è individuabile.
    /// </summary>
    private static int FindBodyEnd(IReadOnlyList<string> texts)
    {
        var total = ReceiptHeaderParser.FindTotal(texts);
        if (!total.Found)
        {
            return -1;
        }

        var end = total.LineIndex;
        for (var i = 0; i < end; i++)
        {
            var upper = texts[i].ToUpperInvariant();
            if (upper.Contains("SUBTOT", StringComparison.Ordinal) ||
                upper.Contains("SUB TOT", StringComparison.Ordinal))
            {
                return i;
            }
        }

        return end;
    }

    /// <summary>Colonne individuate sullo scontrino intero.</summary>
    private readonly record struct Columns(
        double PriceLeft,
        double? VatLeft,
        double? VatRight,
        double? CodeLeft,
        double? CodeRight);

    /// <summary>
    /// Individua la colonna dei prezzi — e, quando ci sono, quelle dell'aliquota e del codice di
    /// reparto — raggruppando i frammenti per bordo destro sullo scontrino intero.
    /// </summary>
    private static Columns? ResolveColumns(IReadOnlyList<ReceiptVisualLine> body)
    {
        var width = body.Max(l => l.Bounds.Right) - body.Min(l => l.Bounds.Left);
        if (width <= 0)
        {
            return null;
        }

        var tolerance = width * ColumnTolerance;

        var amounts = body
            .SelectMany(l => l.Fragments)
            .Where(f => ReceiptAmount.IsAmountOnly(f.Text, out _))
            .ToList();

        if (amounts.Count == 0)
        {
            return null;
        }

        var clusters = Cluster(amounts, tolerance);

        // La colonna dei prezzi è la più a destra tra quelle di importi. Eccezione dichiarata:
        // se tutti i suoi valori sono aliquote note, quella colonna è l'IVA stampata a destra
        // del prezzo, e i prezzi sono nella colonna precedente.
        var priceIndex = clusters.Count - 1;
        if (clusters.Count > 1 && IsRateColumn(clusters[priceIndex]))
        {
            priceIndex--;
        }

        var price = clusters[priceIndex];

        double? vatLeft = null;
        double? vatRight = null;
        foreach (var cluster in clusters)
        {
            if (cluster != price && IsRateColumn(cluster))
            {
                vatLeft = cluster.Min(f => f.Bounds.Left);
                vatRight = cluster.Max(f => f.Bounds.Right);
            }
        }

        var priceLeft = price.Min(f => f.Bounds.Left);
        var codes = body
            .SelectMany(l => l.Fragments)
            .Where(f => CodePattern.IsMatch(f.Text) && f.Bounds.Right <= priceLeft)
            .ToList();

        double? codeLeft = null;
        double? codeRight = null;
        if (codes.Count >= 2)
        {
            // Solo una colonna vera di codici conta: una cifra isolata dentro una descrizione
            // non si allinea con nulla, e non deve diventare un reparto.
            var codeClusters = Cluster(codes, tolerance);
            var best = codeClusters.MaxBy(c => c.Count);
            if (best is { Count: >= 2 })
            {
                codeLeft = best.Min(f => f.Bounds.Left);
                codeRight = best.Max(f => f.Bounds.Right);
            }
        }

        return new Columns(priceLeft, vatLeft, vatRight, codeLeft, codeRight);
    }

    /// <summary>Raggruppa i frammenti per bordo destro, da sinistra a destra.</summary>
    private static List<List<OcrLine>> Cluster(IReadOnlyList<OcrLine> fragments, double tolerance)
    {
        var clusters = new List<List<OcrLine>>();
        foreach (var fragment in fragments.OrderBy(f => f.Bounds.Right))
        {
            var last = clusters.Count > 0 ? clusters[^1] : null;
            if (last is not null && fragment.Bounds.Right - last.Max(f => f.Bounds.Right) <= tolerance)
            {
                last.Add(fragment);
            }
            else
            {
                clusters.Add([fragment]);
            }
        }

        return clusters;
    }

    /// <summary>
    /// Vero se la colonna contiene <b>solo</b> aliquote note. L'insieme chiuso delle aliquote
    /// italiane è ciò che rende decidibile un caso altrimenti ambiguo: <c>10,00</c> da solo può
    /// essere un prezzo, una colonna di soli <c>4,00 / 10,00 / 22,00</c> no.
    /// </summary>
    private static bool IsRateColumn(IReadOnlyList<OcrLine> cluster)
    {
        if (cluster.Count < 2)
        {
            return false;
        }

        foreach (var fragment in cluster)
        {
            if (!ReceiptAmount.IsAmountOnly(fragment.Text, out var cents) ||
                !ReceiptVatSummaryParser.KnownRates.Contains((int)cents))
            {
                return false;
            }
        }

        return true;
    }

    private static List<ReceiptItemLine> BuildItems(
        IReadOnlyList<ReceiptVisualLine> body,
        Columns columns,
        ReceiptVatSummary summary)
    {
        var items = new List<ReceiptItemLine>();
        Qualifier? pending = null;
        var started = false;

        foreach (var line in body)
        {
            var price = FindPriceFragment(line, columns);

            if (price is null)
            {
                if (!started)
                {
                    // Siamo ancora nell'intestazione: il corpo comincia al primo importo in colonna.
                    continue;
                }

                var standalone = ReadQualifier(line.Text);
                if (standalone is not null)
                {
                    pending = ApplyQualifier(items, standalone.Value) ? null : standalone;
                    continue;
                }

                if (IsServiceLine(line.Text))
                {
                    continue;
                }

                AppendContinuation(items, line.Text);
                continue;
            }

            started = true;

            var vat = ReadVatRate(line, columns, price.Value, summary);
            var description = BuildDescription(line, columns, price.Value, vat.Fragment);

            if (IsServiceLine(description))
            {
                continue;
            }

            // La quantità può stare sulla riga del prodotto o su una riga a sé: gli scontrini
            // fanno entrambe le cose, a volte nello stesso scontrino.
            var inline = ReadQualifier(description);
            if (inline is not null)
            {
                description = StripQualifier(description);
            }

            _ = ReceiptAmount.IsAmountOnly(price.Value.Text, out var amount);
            var item = BuildItem(description, amount, vat.Rate, items.Count);

            var qualifier = inline ?? pending;
            if (qualifier is not null)
            {
                item = WithQuantity(item, qualifier.Value);
            }

            pending = null;
            items.Add(item);
        }

        return items;
    }

    /// <summary>Frammento del prezzo: l'importo più a destra che cade nella colonna dei prezzi.</summary>
    private static OcrLine? FindPriceFragment(ReceiptVisualLine line, Columns columns)
    {
        OcrLine? found = null;
        foreach (var fragment in line.Fragments)
        {
            if (fragment.Bounds.Left + 1 < columns.PriceLeft)
            {
                continue;
            }

            if (!ReceiptAmount.IsAmountOnly(fragment.Text, out _))
            {
                continue;
            }

            if (found is null || fragment.Bounds.Right > found.Value.Bounds.Right)
            {
                found = fragment;
            }
        }

        return found;
    }

    /// <summary>
    /// Aliquota della riga: dalla colonna dell'IVA se c'è, altrimenti dal codice di reparto
    /// risolto con il riepilogo. Senza corrispondenza resta <c>null</c>: non si deduce.
    /// </summary>
    private static (int? Rate, OcrLine? Fragment) ReadVatRate(
        ReceiptVisualLine line,
        Columns columns,
        OcrLine price,
        ReceiptVatSummary summary)
    {
        if (columns.VatLeft is not null)
        {
            foreach (var fragment in line.Fragments)
            {
                if (fragment.Bounds.Right <= columns.VatRight + 1 &&
                    fragment.Bounds.Left + 1 >= columns.VatLeft &&
                    ReceiptAmount.IsAmountOnly(fragment.Text, out var cents) &&
                    ReceiptVatSummaryParser.KnownRates.Contains((int)cents))
                {
                    return ((int)cents, fragment);
                }
            }
        }

        if (columns.CodeLeft is not null)
        {
            foreach (var fragment in line.Fragments)
            {
                if (fragment.Bounds.Right > price.Bounds.Left ||
                    fragment.Bounds.Right > columns.CodeRight + 1 ||
                    fragment.Bounds.Left + 1 < columns.CodeLeft)
                {
                    continue;
                }

                var code = CodePattern.Match(fragment.Text);
                if (!code.Success)
                {
                    continue;
                }

                // Codice presente ma non nel riepilogo: la riga resta senza aliquota, e il
                // frammento esce comunque dalla descrizione perché non è parte del nome.
                return (summary.RateForCode(code.Groups[1].Value), fragment);
            }
        }

        return (null, null);
    }

    /// <summary>Descrizione: tutto ciò che sta a sinistra del prezzo, tolti prezzo e aliquota.</summary>
    private static string BuildDescription(
        ReceiptVisualLine line,
        Columns columns,
        OcrLine price,
        OcrLine? vat)
    {
        var parts = new List<string>();
        foreach (var fragment in line.Fragments)
        {
            if (fragment.Bounds.Left >= columns.PriceLeft - 1)
            {
                continue;
            }

            if (fragment.Equals(price) || (vat is not null && fragment.Equals(vat.Value)))
            {
                continue;
            }

            var text = fragment.Text.Trim();
            if (text.Length > 0)
            {
                parts.Add(text);
            }
        }

        return string.Join(" ", parts).Trim();
    }

    /// <summary>Quantità e prezzo unitario letti da una riga o da una riga a sé.</summary>
    private readonly record struct Qualifier(long QuantityMilli, ReceiptItemUnit Unit, long UnitPriceCents);

    /// <summary>
    /// Legge <c>2 X 1,50</c> o <c>0,432 kg x 2,99</c>. La quantità va in millesimi interi: i
    /// grammi ci finiscono così come sono, i chili moltiplicati per mille.
    /// </summary>
    private static Qualifier? ReadQualifier(string text)
    {
        var match = QuantityPattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var quantityText = match.Groups["qty"].Value.Replace(',', '.');
        if (!decimal.TryParse(quantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) ||
            quantity <= 0)
        {
            return null;
        }

        if (!ReceiptAmount.IsAmountOnly(match.Groups["price"].Value.Replace('.', ','), out var unitPrice) ||
            unitPrice <= 0)
        {
            return null;
        }

        var unitToken = match.Groups["unit"].Value.ToUpperInvariant();
        var perKilo = PerKiloPattern.IsMatch(text);

        var isGrams = unitToken is "G" or "GR";
        var isKilos = unitToken == "KG" || (perKilo && !isGrams);

        long milli;
        ReceiptItemUnit unit;
        if (isGrams)
        {
            milli = (long)Math.Round(quantity, MidpointRounding.AwayFromZero);
            unit = ReceiptItemUnit.Kilogram;
        }
        else if (isKilos)
        {
            milli = (long)Math.Round(quantity * 1000m, MidpointRounding.AwayFromZero);
            unit = ReceiptItemUnit.Kilogram;
        }
        else
        {
            milli = (long)Math.Round(quantity * 1000m, MidpointRounding.AwayFromZero);
            unit = ReceiptItemUnit.Piece;
        }

        return milli <= 0 ? null : new Qualifier(milli, unit, unitPrice);
    }

    /// <summary>
    /// Toglie dalla descrizione il pezzo che esprime la quantità: <c>2 X 0,89</c> è un dato
    /// della riga, non parte del nome del prodotto, e lasciarlo dentro impedirebbe di
    /// raggruppare lo stesso prodotto comprato in quantità diverse.
    /// </summary>
    private static string StripQualifier(string description)
    {
        var stripped = QuantityPattern.Replace(description, " ", 1);
        return string.Join(' ', stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    /// <summary>
    /// Attacca la quantità letta su una riga a sé al prodotto già letto. Vero se l'ha attaccata:
    /// altrimenti resta in attesa del prodotto che segue, perché gli scontrini stampano la
    /// quantità tanto sopra quanto sotto la descrizione.
    /// </summary>
    private static bool ApplyQualifier(List<ReceiptItemLine> items, Qualifier qualifier)
    {
        if (items.Count == 0)
        {
            return false;
        }

        var last = items[^1];
        if (last.UnitPriceCents is not null || last.Kind == ReceiptItemKind.Discount)
        {
            return false;
        }

        items[^1] = WithQuantity(last, qualifier);
        return true;
    }

    /// <summary>
    /// Applica quantità e prezzo unitario a una riga, verificando il totale già stampato. Se non
    /// coincide la riga si marca incoerente: si segnala, non si corregge.
    /// </summary>
    private static ReceiptItemLine WithQuantity(ReceiptItemLine item, Qualifier qualifier)
    {
        var expected = (long)Math.Round(
            qualifier.QuantityMilli * qualifier.UnitPriceCents / 1000m,
            MidpointRounding.AwayFromZero);

        var sign = item.AmountCents < 0 ? -1 : 1;
        var inconsistent = item.AmountCents != 0 && Math.Abs(item.AmountCents) != expected;
        var amount = item.AmountCents == 0 ? expected * sign : item.AmountCents;

        return item with
        {
            QuantityMilli = qualifier.QuantityMilli,
            Unit = qualifier.Unit,
            UnitPriceCents = qualifier.UnitPriceCents,
            AmountCents = amount,
            IsInconsistent = inconsistent,
        };
    }

    /// <summary>
    /// Riga senza importo che segue un prodotto: è il seguito della descrizione. Senza questa
    /// regola diventerebbe un prodotto fantasma a prezzo zero.
    /// </summary>
    private static void AppendContinuation(List<ReceiptItemLine> items, string text)
    {
        var addition = text.Trim();
        if (items.Count == 0 || addition.Length == 0)
        {
            return;
        }

        var last = items[^1];
        var description = $"{last.RawDescription} {addition}".Trim();
        items[^1] = last with
        {
            RawDescription = description,
            NormalizedDescription = TextNormalizer.Normalize(description),
        };
    }

    /// <summary>Costruisce la riga, decidendo se è un prodotto o uno sconto.</summary>
    private static ReceiptItemLine BuildItem(string description, long amount, int? vatRate, int order)
    {
        var upper = description.ToUpperInvariant();
        var marked = DiscountKeywords.Any(k => upper.Contains(k, StringComparison.Ordinal));
        var isDiscount = marked || amount < 0;

        // Uno sconto stampato senza segno resta uno sconto: l'importo va in negativo, altrimenti
        // la somma delle righe supererebbe il totale proprio dove lo scontrino dice il contrario.
        if (isDiscount && amount > 0)
        {
            amount = -amount;
        }

        return new ReceiptItemLine(
            description,
            TextNormalizer.Normalize(description),
            ReceiptItemLine.SingleUnit,
            ReceiptItemUnit.Piece,
            null,
            amount,
            vatRate,
            isDiscount ? ReceiptItemKind.Discount : ReceiptItemKind.Product,
            false,
            order);
    }

    /// <summary>Riga di servizio: reparto, conteggio pezzi, riepilogo. Non è un prodotto.</summary>
    private static bool IsServiceLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var upper = text.ToUpperInvariant();
        return ServiceKeywords.Any(k => upper.Contains(k, StringComparison.Ordinal));
    }
}
