namespace CardMaster.Data;

/// <summary>
/// Carta fedeltà locale al device. In questa change (maui-shell) i campi sono
/// essenziali per validare lo schema; l'arricchimento avviene con maui-scan-card.
/// </summary>
public class Card : EntityBase
{
    /// <summary>Emittente riconosciuto (dal catalogo seed), se noto.</summary>
    public string? IssuerName { get; set; }

    /// <summary>Nome mostrato all'utente.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Contenuto del codice a barre (immutabile).</summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>Formato del barcode (es. EAN13, CODE128, QR_CODE).</summary>
    public string BarcodeFormat { get; set; } = string.Empty;

    /// <summary>Colore associato (hex), opzionale.</summary>
    public string? Color { get; set; }

    /// <summary>Id del logo nel catalogo emittenti, opzionale.</summary>
    public string? LogoId { get; set; }
}
