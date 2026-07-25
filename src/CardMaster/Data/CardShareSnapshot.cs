namespace CardMaster.Data;

/// <summary>
/// Snapshot autosufficiente di una carta, scambiato tramite QR di condivisione
/// (capability card-sharing). Contiene tutti i dati per ricostruire una COPIA
/// indipendente sull'altro device: nessun Id di origine, nessun riferimento remoto.
/// </summary>
/// <param name="DisplayName">Nome mostrato (può essere vuoto: il ricevente lo completa).</param>
/// <param name="IssuerName">Emittente, se presente.</param>
/// <param name="Barcode">Valore del codice a barre.</param>
/// <param name="BarcodeFormat">Formato stabile (es. EAN13, QR_CODE).</param>
/// <param name="Color">Colore hex associato, se presente.</param>
/// <param name="LogoId">Riferimento logo del catalogo, se presente (solo la stringa, non il binario).</param>
public sealed record CardShareSnapshot(
    string DisplayName,
    string? IssuerName,
    string Barcode,
    string BarcodeFormat,
    string? Color,
    string? LogoId);
