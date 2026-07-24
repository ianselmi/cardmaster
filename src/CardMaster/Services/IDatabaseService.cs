using SQLite;

namespace CardMaster.Services;

/// <summary>
/// Gestisce la connessione al database SQLite cifrato (SQLCipher) e la sua
/// inizializzazione idempotente all'avvio.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Restituisce la connessione cifrata, inizializzando il database (apertura con
    /// passphrase, creazione schema, versione) al primo accesso. Idempotente.
    /// </summary>
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}
