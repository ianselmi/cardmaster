using System.Text.Json;
using System.Text.Json.Serialization;
using CardMaster.Data;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Accesso al dizionario delle categorie di spesa (seed statico bundle nell'app). Read-only,
/// offline, senza sync — stesso modello del catalogo emittenti.
/// </summary>
public interface ICategoryCatalog
{
    /// <summary>Categorie del dizionario.</summary>
    Task<IReadOnlyList<ProductCategory>> GetAllAsync();

    /// <summary>Nome mostrabile di una categoria, oppure null se l'id non esiste.</summary>
    Task<string?> GetNameAsync(string? id);
}

/// <summary>
/// Carica il dizionario dal seed bundle <c>Resources/Raw/categories.json</c>.
/// Caricamento lazy e idempotente, come <c>IssuerCatalog</c>.
/// </summary>
public sealed class CategoryCatalog : ICategoryCatalog
{
    private const string SeedAsset = "categories.json";
    private const int SupportedVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    private IReadOnlyList<ProductCategory>? _categories;
    private Dictionary<string, ProductCategory>? _byId;

    public async Task<IReadOnlyList<ProductCategory>> GetAllAsync()
    {
        await EnsureLoadedAsync().ConfigureAwait(false);
        return _categories!;
    }

    public async Task<string?> GetNameAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        await EnsureLoadedAsync().ConfigureAwait(false);
        return _byId!.TryGetValue(id.Trim(), out var category) ? category.Name : null;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_categories is not null)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_categories is not null)
            {
                return;
            }

            SeedFile seed;
            try
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync(SeedAsset).ConfigureAwait(false);
                seed = await JsonSerializer.DeserializeAsync<SeedFile>(stream, JsonOptions).ConfigureAwait(false)
                       ?? throw new InvalidOperationException($"Seed categorie '{SeedAsset}' vuoto o non valido.");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Impossibile caricare il seed categorie '{SeedAsset}'.", ex);
            }

            if (seed.Version != SupportedVersion)
            {
                throw new InvalidOperationException(
                    $"Versione del seed categorie non supportata: {seed.Version} (attesa {SupportedVersion}).");
            }

            var categories = seed.Categories ?? [];
            _byId = categories
                .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                .ToDictionary(c => c.Id, StringComparer.Ordinal);
            _categories = categories;
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class SeedFile
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("categories")]
        public List<ProductCategory>? Categories { get; set; }
    }
}
