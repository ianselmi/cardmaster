namespace CardMaster.Services.Receipts;

/// <summary>Che cosa è una riga del corpo dello scontrino.</summary>
public enum ReceiptItemKind
{
    /// <summary>Prodotto acquistato.</summary>
    Product = 0,

    /// <summary>Sconto o promozione: importo negativo, non un prodotto.</summary>
    Discount = 1,
}

/// <summary>
/// Unità della quantità. Senza questa distinzione <c>2 pz</c> e <c>0,002 kg</c> sono lo stesso
/// numero di millesimi, e i prezzi unitari non sono confrontabili tra uno scontrino e l'altro.
/// </summary>
public enum ReceiptItemUnit
{
    /// <summary>Pezzi.</summary>
    Piece = 0,

    /// <summary>Peso in chilogrammi.</summary>
    Kilogram = 1,
}

/// <summary>
/// Riga del corpo dello scontrino, ricostruita dalla geometria dell'OCR.
/// <para>
/// Tutto in interi: importi in centesimi, quantità in millesimi, aliquota in punti base
/// (<c>4,00%</c> → <c>400</c>). Nessuna virgola mobile lungo il percorso, come già per il
/// totale — un errore di arrotondamento qui non fa rumore, sposta solo una quadratura.
/// </para>
/// </summary>
/// <param name="RawDescription">Descrizione come la stampa lo scontrino, abbreviazioni comprese.</param>
/// <param name="NormalizedDescription">
/// Descrizione normalizzata con <c>TextNormalizer</c>, la stessa regola di ricerca e label.
/// Sta <b>sulla riga</b> e non solo nella tabella delle mappature: le serie storiche di prezzo
/// non devono dipendere da una tabella che l'utente può riscrivere.
/// </param>
/// <param name="QuantityMilli">Quantità in millesimi (1 pezzo → 1000, 0,432 kg → 432).</param>
/// <param name="Unit">Pezzi o peso.</param>
/// <param name="UnitPriceCents">Prezzo unitario, <c>null</c> quando lo scontrino non lo stampa.</param>
/// <param name="AmountCents">Importo della riga; negativo per gli sconti.</param>
/// <param name="VatRateBasisPoints">
/// Aliquota in punti base, <c>null</c> quando non è leggibile. Mai dedotta dalla categoria e
/// mai assunta per default: un'aliquota inventata è indistinguibile da una letta.
/// </param>
/// <param name="Kind">Prodotto o sconto.</param>
/// <param name="IsInconsistent">
/// Vero quando lo scontrino riporta sia quantità per prezzo unitario sia il totale di riga, e i
/// due non coincidono. Si segnala, non si corregge.
/// </param>
/// <param name="Order">Posizione nello scontrino, per ripresentare le righe nell'ordine stampato.</param>
public readonly record struct ReceiptItemLine(
    string RawDescription,
    string NormalizedDescription,
    long QuantityMilli,
    ReceiptItemUnit Unit,
    long? UnitPriceCents,
    long AmountCents,
    int? VatRateBasisPoints,
    ReceiptItemKind Kind,
    bool IsInconsistent,
    int Order)
{
    /// <summary>Quantità implicita: una unità. Non si inventa nulla, si assume uno.</summary>
    public const long SingleUnit = 1000;

    /// <summary>Riga prodotto con quantità implicita.</summary>
    public static ReceiptItemLine Product(string description, long amountCents, int order) =>
        new(
            description,
            TextNormalizer.Normalize(description),
            SingleUnit,
            ReceiptItemUnit.Piece,
            null,
            amountCents,
            null,
            ReceiptItemKind.Product,
            false,
            order);
}
