using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardMaster.Services.Receipts;

// DTO della risposta del modello. I nomi dei campi e le unità sono quelli imposti dallo schema
// in ReceiptAiSchema: importi in centesimi interi, quantità in millesimi, aliquote in punti base.
// La conversione nei tipi del dominio sta in ReceiptAiMapper, che è logica pura e testabile.

internal sealed class ReceiptAiResponseDto
{
    [JsonPropertyName("merchant_name")] public string? MerchantName { get; set; }
    [JsonPropertyName("merchant_vat_id")] public string? MerchantVatId { get; set; }
    [JsonPropertyName("purchased_at")] public string? PurchasedAt { get; set; }
    [JsonPropertyName("total_cents")] public long? TotalCents { get; set; }
    [JsonPropertyName("tax_cents")] public long? TaxCents { get; set; }
    [JsonPropertyName("items")] public List<ReceiptAiItemDto>? Items { get; set; }
    [JsonPropertyName("vat_summary")] public List<ReceiptAiVatEntryDto>? VatSummary { get; set; }
}

internal sealed class ReceiptAiItemDto
{
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("quantity_milli")] public long? QuantityMilli { get; set; }
    [JsonPropertyName("unit")] public string? Unit { get; set; }
    [JsonPropertyName("unit_price_cents")] public long? UnitPriceCents { get; set; }
    [JsonPropertyName("amount_cents")] public long? AmountCents { get; set; }
    [JsonPropertyName("vat_rate_basis_points")] public int? VatRateBasisPoints { get; set; }
    [JsonPropertyName("kind")] public string? Kind { get; set; }
}

internal sealed class ReceiptAiVatEntryDto
{
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("rate_basis_points")] public int? RateBasisPoints { get; set; }
    [JsonPropertyName("taxable_cents")] public long? TaxableCents { get; set; }
    [JsonPropertyName("tax_cents")] public long? TaxCents { get; set; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ReceiptAiResponseDto))]
internal sealed partial class ReceiptAiJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Schema della risposta, <b>imposto</b> nella richiesta via <c>output_config.format</c> e non
/// chiesto nel prompt e sperato. Il parsing a valle non è difensivo perché non deve esserlo: se
/// lo schema è rispettato i campi ci sono e sono del tipo giusto.
/// <para>
/// Le unità sono quelle del dominio — centesimi, millesimi, punti base — così l'esito del modello
/// entra nelle stesse strutture del parser locale senza passare per la virgola mobile.
/// </para>
/// </summary>
internal static class ReceiptAiSchema
{
    /// <summary>
    /// Un campo che il modello può lasciare vuoto. Scritto con <c>anyOf</c> e non con un elenco
    /// di tipi perché <c>anyOf</c> è tra i costrutti dichiarati supportati dagli output strutturati.
    /// </summary>
    private const string NullableInteger = """{"anyOf":[{"type":"integer"},{"type":"null"}]}""";
    private const string NullableString = """{"anyOf":[{"type":"string"},{"type":"null"}]}""";

    public const string Json = $$"""
    {
      "type": "object",
      "additionalProperties": false,
      "required": ["merchant_name", "merchant_vat_id", "purchased_at", "total_cents", "tax_cents", "items", "vat_summary"],
      "properties": {
        "merchant_name": {{NullableString}},
        "merchant_vat_id": {{NullableString}},
        "purchased_at": {{NullableString}},
        "total_cents": {{NullableInteger}},
        "tax_cents": {{NullableInteger}},
        "items": {
          "type": "array",
          "items": {
            "type": "object",
            "additionalProperties": false,
            "required": ["description", "quantity_milli", "unit", "unit_price_cents", "amount_cents", "vat_rate_basis_points", "kind"],
            "properties": {
              "description": {"type": "string"},
              "quantity_milli": {"type": "integer"},
              "unit": {"type": "string", "enum": ["piece", "kilogram"]},
              "unit_price_cents": {{NullableInteger}},
              "amount_cents": {"type": "integer"},
              "vat_rate_basis_points": {{NullableInteger}},
              "kind": {"type": "string", "enum": ["product", "discount"]}
            }
          }
        },
        "vat_summary": {
          "type": "array",
          "items": {
            "type": "object",
            "additionalProperties": false,
            "required": ["code", "rate_basis_points", "taxable_cents", "tax_cents"],
            "properties": {
              "code": {{NullableString}},
              "rate_basis_points": {"type": "integer"},
              "taxable_cents": {{NullableInteger}},
              "tax_cents": {{NullableInteger}}
            }
          }
        }
      }
    }
    """;

    /// <summary>Lo schema nella forma che l'SDK vuole: proprietà di primo livello a dizionario.</summary>
    public static Dictionary<string, JsonElement> ToDictionary()
    {
        using var document = JsonDocument.Parse(Json);
        return document.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }
}

/// <summary>
/// Istruzioni per la rilettura. Descrivono <b>cosa</b> leggere e le convenzioni dello scontrino
/// italiano; il <b>formato</b> non è chiesto qui, lo impone lo schema.
/// </summary>
internal static class ReceiptAiPrompt
{
    public const string System = """
    Leggi lo scontrino fiscale italiano nella foto e riportane il contenuto.

    Convenzioni di questi scontrini:
    - I prezzi stampati sono IVA inclusa.
    - Il corpo elenca un prodotto per riga: descrizione a sinistra, importo a destra, spesso con
      un codice di reparto in colonna. Due prodotti stampati vicini restano due righe distinte.
    - Una riga di sconto o promozione porta un importo negativo e non è un prodotto.
    - Le righe a peso indicano quantità per prezzo al chilo.
    - Il riepilogo IVA a piè di scontrino associa ogni reparto alla sua aliquota.

    Come riportare:
    - Una voce per ogni riga del corpo, nell'ordine di stampa.
    - Importi in centesimi interi: 12,34 € vale 1234. Gli sconti sono negativi.
    - Quantità in millesimi: un pezzo vale 1000, 0,432 kg vale 432.
    - Aliquote in punti base: 4% vale 400, 22% vale 2200.
    - L'aliquota di una riga si legge dal riepilogo tramite il codice di reparto quando è stampata
      in colonna; se non è ricavabile, lascia il campo vuoto.
    - Data e ora nel formato AAAA-MM-GGTHH:MM, ora locale come stampata.

    Ciò che non riesci a leggere va lasciato vuoto. Non dedurre, non calcolare per far quadrare i
    conti, non inventare un valore plausibile: un dato inventato è indistinguibile da uno letto, e
    un campo vuoto si corregge a mano mentre uno sbagliato passa inosservato.
    """;

    public const string User = "Riporta il contenuto di questo scontrino.";
}
