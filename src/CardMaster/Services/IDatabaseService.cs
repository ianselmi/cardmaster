using SQLite;

namespace CardMaster.Services;

/// <summary>
/// Gestisce la connessione al database SQLite locale (in chiaro, v1) e la sua
/// inizializzazione idempotente all'avvio. Espone snapshot e sostituzione del file per
/// abilitare il backup/ripristino su Google Drive.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Restituisce la connessione, inizializzando il database (apertura, creazione schema,
    /// versione) al primo accesso. Idempotente e thread-safe.
    /// </summary>
    Task<SQLiteAsyncConnection> GetConnectionAsync();

    /// <summary>Versione di schema supportata dall'app installata (guardia anti-downgrade nel restore).</summary>
    int CurrentSchemaVersion { get; }

    /// <summary>
    /// Produce uno snapshot transazionalmente consistente e compattato del database su
    /// <paramref name="destinationPath"/> tramite <c>VACUUM INTO</c>, senza chiudere la
    /// connessione né bloccare l'app.
    /// </summary>
    Task SnapshotAsync(string destinationPath);

    /// <summary>Chiude e azzera la connessione singleton in modo atomico. Idempotente.</summary>
    Task CloseAsync();

    /// <summary>
    /// Sostituisce il file del database con il contenuto di <paramref name="sourcePath"/>
    /// (chiusura → swap del file → riapertura lazy con ri-applicazione di schema/versione).
    /// </summary>
    Task ReplaceFromAsync(string sourcePath);
}
