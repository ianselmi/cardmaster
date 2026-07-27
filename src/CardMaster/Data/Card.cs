using SQLite;

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

    /// <summary>
    /// Colore di <b>brand</b> (hex) ereditato dal catalogo emittenti o da un QR ricevuto, opzionale.
    /// NON è il colore del riquadro nella lista: per quello vedi <see cref="TileColor"/>.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Colore del <b>riquadro</b> nella lista (hex), scelto esplicitamente dall'utente dalla
    /// palette. <c>null</c> = automatico, cioè derivato dal nome (comportamento di default).
    /// Distinto da <see cref="Color"/> proprio perché la scelta dell'utente deve vincere
    /// sull'automatismo senza essere sovrascritta dall'arricchimento dell'emittente.
    /// </summary>
    public string? TileColor { get; set; }

    /// <summary>
    /// Label della carta serializzate (separatore <see cref="CardLabels.Separator"/>), opzionale.
    /// Colonna persistita: usare <see cref="Labels"/> per lavorarci in modo tipizzato.
    /// </summary>
    public string? LabelsCsv { get; set; }

    /// <summary>Label della carta come lista. Legge e scrive <see cref="LabelsCsv"/>.</summary>
    [Ignore]
    public List<string> Labels
    {
        get => CardLabels.Parse(LabelsCsv);
        set => LabelsCsv = CardLabels.Serialize(value);
    }

    /// <summary>Id del logo nel catalogo emittenti, opzionale.</summary>
    public string? LogoId { get; set; }

    /// <summary>Data/ora dell'ultima apertura della carta (pagina barcode), se mai aperta.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}
