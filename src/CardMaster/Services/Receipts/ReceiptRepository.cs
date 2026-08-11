using CardMaster.Data;

namespace CardMaster.Services.Receipts;

/// <inheritdoc />
public sealed class ReceiptRepository : IReceiptRepository
{
    private readonly IDatabaseService _database;

    public ReceiptRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Receipt>> GetAllAsync()
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var receipts = await connection.Table<Receipt>()
            .Where(r => r.DeletedAt == null)
            .ToListAsync()
            .ConfigureAwait(false);

        // Ordinamento in memoria: la data d'acquisto può mancare (scontrino incompleto) e in
        // quel caso vale la data di acquisizione, cosa che SQLite non esprime in una Table query.
        return receipts
            .OrderByDescending(r => r.PurchasedAt ?? r.CreatedAt)
            .ToList();
    }

    public async Task<Receipt?> GetByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<Receipt>()
            .Where(r => r.Id == id && r.DeletedAt == null)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Receipt receipt)
    {
        if (string.IsNullOrEmpty(receipt.Id))
        {
            receipt.Id = Guid.NewGuid().ToString();
        }

        var now = DateTimeOffset.UtcNow;
        receipt.CreatedAt = now;
        receipt.UpdatedAt = now;

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.InsertAsync(receipt).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Receipt receipt)
    {
        receipt.UpdatedAt = DateTimeOffset.UtcNow;

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.UpdateAsync(receipt).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        var receipt = await GetByIdAsync(id).ConfigureAwait(false);
        if (receipt is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        receipt.DeletedAt = now;
        receipt.UpdatedAt = now;

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.UpdateAsync(receipt).ConfigureAwait(false);
    }
}
