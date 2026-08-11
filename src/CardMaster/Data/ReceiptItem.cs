using CardMaster.Services.Receipts;
using SQLite;

namespace CardMaster.Data;

/// <summary>
/// Riga di uno scontrino: un prodotto acquistato o uno sconto.
/// <para>
/// Tabella vera e non colonna serializzata dentro lo scontrino — a differenza delle label delle
/// carte — perché le viste di analisi dovranno fare <c>GROUP BY</c> su prodotti e categorie e
/// leggere serie storiche di prezzi. Con le righe dentro una stringa, ogni vista diventerebbe
/// una scansione in memoria di tutto lo storico.
/// </para>
/// <para>
/// Le righe <b>appartengono</b> allo scontrino: si sostituiscono in blocco quando lo scontrino
/// viene modificato e diventano tombstone insieme a lui.
/// </para>
/// </summary>
public class ReceiptItem : EntityBase
{
    /// <summary>Scontrino di appartenenza. Indicizzato: le analisi leggono per scontrino e in blocco.</summary>
    [Indexed]
    public string ReceiptId { get; set; } = string.Empty;

    /// <summary>Descrizione come la stampa lo scontrino, abbreviazioni comprese.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione normalizzata, la chiave con cui lo stesso prodotto si riconosce tra scontrini
    /// diversi. Sta qui e non solo in <see cref="ProductMapping"/> perché le serie storiche non
    /// devono dipendere da una tabella che l'utente può riscrivere correggendo una categoria.
    /// </summary>
    [Indexed]
    public string NormalizedDescription { get; set; } = string.Empty;

    /// <summary>Quantità in millesimi: 1 pezzo → 1000, 0,432 kg → 432.</summary>
    public long QuantityMilli { get; set; } = ReceiptItemLine.SingleUnit;

    /// <summary>Pezzi o peso: senza, <c>2 pz</c> e <c>0,002 kg</c> sono lo stesso numero.</summary>
    public ReceiptItemUnit Unit { get; set; } = ReceiptItemUnit.Piece;

    /// <summary>Prezzo unitario in centesimi, null quando lo scontrino non lo stampa.</summary>
    public long? UnitPriceCents { get; set; }

    /// <summary>Importo della riga in centesimi; negativo per gli sconti.</summary>
    public long AmountCents { get; set; }

    /// <summary>Aliquota IVA in punti base (<c>4,00%</c> → <c>400</c>), null se non letta.</summary>
    public int? VatRateBasisPoints { get; set; }

    /// <summary>Prodotto o sconto.</summary>
    public ReceiptItemKind Kind { get; set; } = ReceiptItemKind.Product;

    /// <summary>Categoria di spesa, null quando nessuna sorgente l'ha riconosciuta.</summary>
    public string? Category { get; set; }

    /// <summary>Posizione nello scontrino, per ripresentare le righe nell'ordine stampato.</summary>
    public int Order { get; set; }

    /// <summary>Vero quando quantità per prezzo unitario non torna con il totale di riga stampato.</summary>
    public bool IsInconsistent { get; set; }
}
