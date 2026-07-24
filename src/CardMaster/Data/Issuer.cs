namespace CardMaster.Data;

/// <summary>
/// Emittente di una carta fedeltà (es. un supermercato). Dato statico del catalogo
/// bundle nell'app (vedi <c>Resources/Raw/issuers.json</c>), read-only e senza sync.
///
/// L'associazione di una carta a un emittente è FACOLTATIVA: il catalogo fornisce
/// suggerimenti, non vincoli. Una carta può avere un emittente del catalogo, uno
/// libero non catalogato, o nessuno.
/// </summary>
public sealed class Issuer
{
    /// <summary>Identificativo stabile (es. "esselunga").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nome visualizzato.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Colore associato in formato hex (es. "#0055A4").</summary>
    public string? ColorHex { get; set; }

    /// <summary>Formato barcode atteso per questo emittente (es. "EAN13"), opzionale.</summary>
    public string? ExpectedBarcodeFormat { get; set; }

    /// <summary>Nome dell'immagine logo bundle in Resources/Images, opzionale.</summary>
    public string? LogoAsset { get; set; }

    /// <summary>Nomi alternativi per il match testuale.</summary>
    public IReadOnlyList<string> Aliases { get; set; } = Array.Empty<string>();
}
