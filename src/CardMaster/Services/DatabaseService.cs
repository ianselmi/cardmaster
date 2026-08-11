using CardMaster.Data;
using SQLite;

namespace CardMaster.Services;

/// <summary>
/// Apre e inizializza il database SQLite locale (in chiaro, v1). L'inizializzazione
/// è idempotente e thread-safe. Espone snapshot (<c>VACUUM INTO</c>) e sostituzione atomica
/// del file per il backup/ripristino su Google Drive.
/// </summary>
public sealed class DatabaseService : IDatabaseService
{
    private const string DatabaseFileName = "cardmaster.db3";

    // v2 (change maui-card-color-labels): colonne TileColor e LabelsCsv su Card. Le colonne le
    // aggiunge CreateTableAsync<Card>() sulle installazioni esistenti (ALTER TABLE ADD COLUMN,
    // valori NULL = comportamento precedente): l'incremento serve alla guardia del backup Drive
    // (BackupNaming.CanRestore), perché un backup -v2 non sia ripristinato da un'app più vecchia.
    //
    // v3 (change receipt-capture): tabella Receipt. La crea CreateTableAsync<Receipt>() sulle
    // installazioni esistenti; l'incremento serve, come sopra, solo alla guardia del ripristino:
    // un backup -v3 contiene scontrini che un'app più vecchia non saprebbe mostrare.
    //
    // v4 (change receipt-items): tabelle ReceiptItem e ProductMapping, più la colonna TaxCents su
    // Receipt. Anche qui non c'è migrazione da scrivere — CreateTableAsync crea le tabelle nuove
    // e aggiunge la colonna mancante a quella esistente — e l'incremento serve solo alla guardia:
    // un backup -v4 porta le righe degli scontrini, che un'app più vecchia perderebbe in silenzio.
    private const int SchemaVersion = 4;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public int CurrentSchemaVersion => SchemaVersion;

    private static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var connection = new SQLiteAsyncConnection(
                DatabasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache,
                storeDateTimeAsTicks: true);

            await connection.CreateTableAsync<Card>().ConfigureAwait(false);
            await connection.CreateTableAsync<Receipt>().ConfigureAwait(false);
            await connection.CreateTableAsync<ReceiptItem>().ConfigureAwait(false);
            await connection.CreateTableAsync<ProductMapping>().ConfigureAwait(false);
            await ApplySchemaVersionAsync(connection).ConfigureAwait(false);

            _connection = connection;
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SnapshotAsync(string destinationPath)
    {
        var connection = await GetConnectionAsync().ConfigureAwait(false);

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        // VACUUM INTO non accetta parametri bind: il path è app-controlled (cache privata),
        // ma raddoppiamo comunque gli apici per sicurezza.
        var escaped = destinationPath.Replace("'", "''");
        await connection.ExecuteAsync($"VACUUM INTO '{escaped}';").ConfigureAwait(false);
    }

    public async Task CloseAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_connection is not null)
            {
                await _connection.CloseAsync().ConfigureAwait(false);
                _connection = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceFromAsync(string sourcePath)
    {
        await CloseAsync().ConfigureAwait(false);

        var dbPath = DatabasePath;
        File.Copy(sourcePath, dbPath, overwrite: true);

        // I sidecar WAL/SHM del vecchio DB non sono più coerenti col file appena sostituito.
        DeleteIfExists(dbPath + "-wal");
        DeleteIfExists(dbPath + "-shm");

        // Riapertura lazy: ricrea la connessione e ri-applica schema/versione come all'avvio.
        await GetConnectionAsync().ConfigureAwait(false);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static async Task ApplySchemaVersionAsync(SQLiteAsyncConnection connection)
    {
        var current = await connection.ExecuteScalarAsync<int>("PRAGMA user_version;").ConfigureAwait(false);
        if (current < SchemaVersion)
        {
            // PRAGMA non accetta parametri: SchemaVersion è una costante interna, non input utente.
            await connection.ExecuteAsync($"PRAGMA user_version = {SchemaVersion};").ConfigureAwait(false);
        }
    }
}
