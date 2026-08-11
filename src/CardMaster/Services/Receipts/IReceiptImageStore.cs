namespace CardMaster.Services.Receipts;

/// <summary>
/// Conserva le immagini degli scontrini nell'area dati privata dell'app, fuori dal database.
/// <para>
/// Fuori dal database di proposito: un BLOB per scontrino gonfierebbe ogni snapshot caricato
/// su Drive, dove la ritenzione è di 3 copie sulla quota dell'utente. La contropartita —
/// le immagini <b>non</b> sono nel backup e non tornano dopo un ripristino — è dichiarata
/// all'utente nella pagina Backup invece di essere scoperta dopo.
/// </para>
/// </summary>
public interface IReceiptImageStore
{
    /// <summary>
    /// Copia l'immagine acquisita nell'area dati dell'app e restituisce il percorso
    /// <b>relativo</b> da salvare sullo scontrino, o null se la copia non riesce.
    /// </summary>
    Task<string?> SaveAsync(string sourcePath, string receiptId, CancellationToken cancellationToken = default);

    /// <summary>Percorso assoluto da un percorso relativo, o null se non esiste più il file.</summary>
    string? ResolveFullPath(string? relativePath);

    /// <summary>Rimuove l'immagine di uno scontrino. Non lancia se il file non c'è più.</summary>
    void Delete(string? relativePath);

    /// <summary>Spazio occupato in byte da tutte le immagini degli scontrini.</summary>
    long GetTotalSizeBytes();

    /// <summary>
    /// Elimina tutte le immagini per liberare spazio. I dati estratti e il testo riconosciuto
    /// degli scontrini restano intatti: è compito del chiamante azzerare i riferimenti.
    /// </summary>
    void DeleteAll();
}
