using CardMaster.Data;
using SQLite;

namespace CardMaster.Services;

/// <summary>
/// Apre e inizializza il database SQLite locale (in chiaro, v1). L'inizializzazione
/// è idempotente e thread-safe.
/// </summary>
public sealed class DatabaseService : IDatabaseService
{
    private const string DatabaseFileName = "cardmaster.db3";
    private const int SchemaVersion = 1;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private SQLiteAsyncConnection? _connection;

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

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

            var connection = new SQLiteAsyncConnection(
                dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache,
                storeDateTimeAsTicks: true);

            await connection.CreateTableAsync<Card>().ConfigureAwait(false);
            await ApplySchemaVersionAsync(connection).ConfigureAwait(false);

            _connection = connection;
            return _connection;
        }
        finally
        {
            _gate.Release();
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
