using CardMaster.Data;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Mappature prodotto → categoria apprese dalle correzioni dell'utente.
/// <para>
/// Sopravvivono agli scontrini da cui sono nate: eliminare lo scontrino su cui si è corretta una
/// categoria non deve far dimenticare la correzione.
/// </para>
/// </summary>
public interface IProductMappingRepository
{
    /// <summary>Tutte le mappature attive, come dizionario descrizione normalizzata → categoria.</summary>
    Task<Dictionary<string, string>> GetLearnedAsync();

    /// <summary>
    /// Registra la categoria scelta dall'utente per una descrizione. Se la mappatura esiste già
    /// la <b>riscrive</b> invece di aggiungerne una seconda: correggere due volte lo stesso
    /// prodotto deve lasciare una sola verità, non due in ordine di inserimento.
    /// </summary>
    Task SetAsync(string normalizedDescription, string category, ProductMappingOrigin origin = ProductMappingOrigin.User);
}

/// <inheritdoc />
public sealed class ProductMappingRepository : IProductMappingRepository
{
    private readonly IDatabaseService _database;

    public ProductMappingRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<Dictionary<string, string>> GetLearnedAsync()
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var mappings = await connection.Table<ProductMapping>()
            .Where(m => m.DeletedAt == null)
            .ToListAsync()
            .ConfigureAwait(false);

        var learned = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            if (!string.IsNullOrWhiteSpace(mapping.NormalizedDescription) &&
                !string.IsNullOrWhiteSpace(mapping.Category))
            {
                learned[mapping.NormalizedDescription] = mapping.Category;
            }
        }

        return learned;
    }

    public async Task SetAsync(
        string normalizedDescription,
        string category,
        ProductMappingOrigin origin = ProductMappingOrigin.User)
    {
        if (string.IsNullOrWhiteSpace(normalizedDescription) || string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        var existing = await connection.Table<ProductMapping>()
            .Where(m => m.NormalizedDescription == normalizedDescription)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (existing is not null)
        {
            // Una scelta dell'utente non viene mai sovrascritta da una prodotta automaticamente:
            // è la regola che rende sicuro dare in pasto questa tabella a un modello, domani.
            if (existing.Origin == ProductMappingOrigin.User && origin != ProductMappingOrigin.User)
            {
                return;
            }

            existing.Category = category;
            existing.Origin = origin;
            existing.DeletedAt = null;
            existing.UpdatedAt = now;
            await connection.UpdateAsync(existing).ConfigureAwait(false);
            return;
        }

        await connection.InsertAsync(new ProductMapping
        {
            NormalizedDescription = normalizedDescription,
            Category = category,
            Origin = origin,
            CreatedAt = now,
            UpdatedAt = now,
        }).ConfigureAwait(false);
    }
}
