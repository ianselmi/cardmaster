using CardMaster.Data;

namespace CardMaster.Services;

/// <inheritdoc />
public sealed class CardRepository : ICardRepository
{
    private readonly IDatabaseService _database;

    public CardRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Card>> GetAllAsync()
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Card>()
            .Where(c => c.DeletedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<Card?> GetByIdAsync(string id)
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Card>()
            .Where(c => c.Id == id && c.DeletedAt == null)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Card card)
    {
        if (string.IsNullOrEmpty(card.Id))
        {
            card.Id = Guid.NewGuid().ToString();
        }

        var now = DateTimeOffset.UtcNow;
        card.CreatedAt = now;
        card.UpdatedAt = now;

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.InsertAsync(card).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Card card)
    {
        card.UpdatedAt = DateTimeOffset.UtcNow;

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.UpdateAsync(card).ConfigureAwait(false);
    }

    public async Task<bool> AnyActiveByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
        {
            return false;
        }

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var existing = await connection.Table<Card>()
            .Where(c => c.Barcode == barcode && c.DeletedAt == null)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return existing is not null;
    }

    public async Task SoftDeleteAsync(string id)
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var card = await connection.Table<Card>()
            .Where(c => c.Id == id && c.DeletedAt == null)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (card is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        card.DeletedAt = now;
        card.UpdatedAt = now;

        // Tombstone: UPDATE, mai DELETE fisico.
        await connection.UpdateAsync(card).ConfigureAwait(false);
    }
}
