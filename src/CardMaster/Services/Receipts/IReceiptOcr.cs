namespace CardMaster.Services.Receipts;

/// <summary>
/// Riga di testo riconosciuta, con il rettangolo che occupa nell'immagine.
/// Le coordinate sono in pixel dell'immagine analizzata, origine in alto a sinistra.
/// </summary>
/// <param name="Text">Testo della riga.</param>
/// <param name="Bounds">Rettangolo occupato dalla riga.</param>
public readonly record struct OcrLine(string Text, Rect Bounds);

/// <summary>
/// Blocco di testo riconosciuto (tipicamente un paragrafo), con le sue righe.
/// </summary>
/// <param name="Text">Testo dell'intero blocco.</param>
/// <param name="Bounds">Rettangolo occupato dal blocco.</param>
/// <param name="Lines">Righe che compongono il blocco, nell'ordine restituito dal motore.</param>
public readonly record struct OcrBlock(string Text, Rect Bounds, IReadOnlyList<OcrLine> Lines);

/// <summary>
/// Esito del riconoscimento del testo di un'immagine.
/// </summary>
/// <param name="Text">Testo integrale riconosciuto, conservato con lo scontrino.</param>
/// <param name="Blocks">Blocchi con la loro geometria.</param>
public readonly record struct OcrResult(string Text, IReadOnlyList<OcrBlock> Blocks)
{
    /// <summary>Nessun testo riconosciuto: esito normale, non un errore.</summary>
    public static OcrResult Empty { get; } = new(string.Empty, []);

    /// <summary>Vero se l'immagine non conteneva testo riconoscibile.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
}

/// <summary>
/// Riconosce il testo di un'immagine <b>interamente sul device</b>: nessuna rete, nessun
/// servizio esterno, nessun download di modelli a runtime (il modello viaggia nell'APK).
/// <para>
/// Restituisce testo <b>e geometria</b>: le coordinate non servono a questa change, che usa
/// solo <see cref="OcrResult.Text"/>, ma sono il presupposto della ricostruzione delle righe
/// prodotto (change <c>receipt-items</c>), dove descrizione e prezzo si separano confrontando
/// le <c>x</c> sulla stessa banda <c>y</c>. Nascono qui per non dover riscrivere il contratto.
/// </para>
/// </summary>
public interface IReceiptOcr
{
    /// <summary>
    /// Analizza l'immagine al percorso indicato. Non lancia per immagini senza testo:
    /// in quel caso restituisce <see cref="OcrResult.Empty"/>.
    /// </summary>
    Task<OcrResult> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementazione inerte per le piattaforme senza motore OCR (l'app è Android-only:
/// esiste per non far fallire la composizione del container su altri target).
/// </summary>
public sealed class NoopReceiptOcr : IReceiptOcr
{
    public Task<OcrResult> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default)
        => Task.FromResult(OcrResult.Empty);
}
