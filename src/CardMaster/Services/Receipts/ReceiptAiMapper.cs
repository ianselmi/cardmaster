using System.Globalization;
using System.Text.Json;
using CardMaster.Services.Ai;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Porta l'esito del modello nelle strutture del dominio. Logica <b>pura</b>: nessuna rete,
/// nessun SDK, nessuna piattaforma — è qui che si annidano gli errori silenziosi (un importo
/// scalato di dieci, una data spostata di un giorno) ed è qui che arrivano i test.
/// </summary>
public static class ReceiptAiMapper
{
    /// <summary>
    /// Formati accettati per data e ora. L'ora è facoltativa: uno scontrino che stampa solo la
    /// data resta leggibile.
    /// </summary>
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd",
    ];

    /// <summary>
    /// Converte il JSON della risposta in una lettura, oppure dichiara
    /// <see cref="AiErrorKind.MalformedResponse"/>.
    /// <para>
    /// Una risposta troncata o incompleta non produce <b>righe parziali</b>: se anche una sola
    /// riga è inutilizzabile fallisce l'intera lettura. Mezzo scontrino presentato come intero
    /// sarebbe peggio di nessuno scontrino — quadrerebbe per caso o sballerebbe senza spiegare
    /// perché, e l'utente non avrebbe modo di accorgersene.
    /// </para>
    /// </summary>
    /// <param name="json">Corpo JSON prodotto dal modello.</param>
    /// <param name="usage">Consumo effettivo riportato dalla risposta.</param>
    /// <param name="now">Istante di riferimento per l'offset locale della data (default: adesso).</param>
    public static ReceiptAiResult Map(string? json, ReceiptAiUsage usage, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ReceiptAiResult.Failed(AiErrorKind.MalformedResponse);
        }

        ReceiptAiResponseDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize(json, ReceiptAiJsonContext.Default.ReceiptAiResponseDto);
        }
        catch (JsonException)
        {
            // Risposta troncata a metà: il JSON non chiude. Errore dichiarato, nessuna riga.
            return ReceiptAiResult.Failed(AiErrorKind.MalformedResponse);
        }

        if (dto is null || dto.Items is null)
        {
            return ReceiptAiResult.Failed(AiErrorKind.MalformedResponse);
        }

        var items = new List<ReceiptItemLine>(dto.Items.Count);
        for (var i = 0; i < dto.Items.Count; i++)
        {
            var line = MapItem(dto.Items[i], i);
            if (line is null)
            {
                return ReceiptAiResult.Failed(AiErrorKind.MalformedResponse);
            }

            items.Add(line.Value);
        }

        var header = new ReceiptHeader(
            NullIfBlank(dto.MerchantName),
            NullIfBlank(dto.MerchantVatId),
            ParsePurchasedAt(dto.PurchasedAt, now ?? DateTimeOffset.Now),
            dto.TotalCents,
            dto.TaxCents);

        return ReceiptAiResult.Ok(new ReceiptAiReading(header, items, MapVatSummary(dto), usage));
    }

    /// <summary>
    /// Una riga, o <c>null</c> se inutilizzabile. Mancano solo i campi che nessuna correzione
    /// manuale potrebbe recuperare: senza descrizione e senza importo non c'è una riga.
    /// </summary>
    private static ReceiptItemLine? MapItem(ReceiptAiItemDto dto, int order)
    {
        var description = NullIfBlank(dto.Description);
        if (description is null || dto.AmountCents is null)
        {
            return null;
        }

        var kind = dto.Kind switch
        {
            "product" => ReceiptItemKind.Product,
            "discount" => ReceiptItemKind.Discount,
            _ => (ReceiptItemKind?)null,
        };

        var unit = dto.Unit switch
        {
            "piece" => ReceiptItemUnit.Piece,
            "kilogram" => ReceiptItemUnit.Kilogram,
            _ => (ReceiptItemUnit?)null,
        };

        if (kind is null || unit is null)
        {
            return null;
        }

        // Quantità assente o non positiva: si assume una unità, come fa il parser locale. È
        // l'unica assunzione ammessa, e non inventa un dato — dichiara che non ce n'era uno.
        var quantity = dto.QuantityMilli is > 0 ? dto.QuantityMilli.Value : ReceiptItemLine.SingleUnit;

        return new ReceiptItemLine(
            description,
            TextNormalizer.Normalize(description),
            quantity,
            unit.Value,
            dto.UnitPriceCents,
            dto.AmountCents.Value,
            dto.VatRateBasisPoints,
            kind.Value,
            IsInconsistent: false,
            order);
    }

    private static ReceiptVatSummary MapVatSummary(ReceiptAiResponseDto dto)
    {
        if (dto.VatSummary is not { Count: > 0 })
        {
            return ReceiptVatSummary.Empty;
        }

        var entries = dto.VatSummary
            .Where(e => e.RateBasisPoints is not null)
            .Select(e => new ReceiptVatEntry(
                NullIfBlank(e.Code),
                e.RateBasisPoints!.Value,
                e.TaxableCents,
                e.TaxCents))
            .ToList();

        return entries.Count == 0
            ? ReceiptVatSummary.Empty
            : new ReceiptVatSummary(entries, dto.TaxCents);
    }

    /// <summary>
    /// Data e ora come <b>stampate</b>, con l'offset locale.
    /// <para>
    /// L'offset non è un dettaglio: sqlite-net persiste <c>DateTimeOffset</c> come tick UTC, e una
    /// mezzanotte locale trattata come UTC torna indietro di un giorno — a fine mese sposterebbe
    /// lo scontrino nel mese sbagliato senza far rumore (vedi <c>docs/technical-notes.md</c>).
    /// </para>
    /// Una data illeggibile lascia il campo vuoto e non fa fallire l'intera lettura: è un campo,
    /// non la struttura della risposta, e l'utente lo corregge nella schermata di conferma.
    /// </summary>
    private static DateTimeOffset? ParsePurchasedAt(string? raw, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTime.TryParseExact(
            raw.Trim(),
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), now.Offset)
            : null;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
