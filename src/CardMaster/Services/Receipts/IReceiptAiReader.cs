namespace CardMaster.Services.Receipts;

/// <summary>
/// Rilegge uno scontrino dalla sua immagine tramite un modello multimodale.
/// <para>
/// Esiste perché il testo dell'OCR ha già perso l'informazione che serve: quando due prodotti
/// finiscono fusi in una riga, l'associazione descrizione↔prezzo non è più nel dato e nessun
/// modello può ricostruirla. L'immagine ce l'ha ancora.
/// </para>
/// <para>
/// <b>Non è la via normale</b>: si usa solo quando la lettura locale non quadra, ed è l'unico
/// punto dell'app in cui un dato dello scontrino lascia il device.
/// </para>
/// </summary>
public interface IReceiptAiReader
{
    /// <summary>
    /// Rilegge l'immagine e restituisce testata e righe, oppure una causa d'errore riconoscibile.
    /// L'immagine viene ridimensionata prima dell'invio; nella richiesta finiscono soltanto quella
    /// foto e le istruzioni, nient'altro dell'utente.
    /// </summary>
    /// <param name="imageBytes">Foto dello scontrino, come acquisita.</param>
    Task<ReceiptAiResult> ReadAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
}
