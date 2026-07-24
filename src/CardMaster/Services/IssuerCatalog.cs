using System.Text.Json;
using System.Text.Json.Serialization;
using CardMaster.Data;

namespace CardMaster.Services;

/// <summary>
/// Carica il catalogo emittenti dal seed bundle <c>Resources/Raw/issuers.json</c>.
/// Caricamento lazy e idempotente (una sola volta), coerente con DatabaseService.
/// </summary>
public sealed class IssuerCatalog : IIssuerCatalog
{
    private const string SeedAsset = "issuers.json";
    private const int SupportedVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    private IReadOnlyList<Issuer>? _issuers;
    private Dictionary<string, Issuer>? _byId;

    public async Task<IReadOnlyList<Issuer>> GetAllAsync()
    {
        await EnsureLoadedAsync().ConfigureAwait(false);
        return _issuers!;
    }

    public async Task<Issuer?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        await EnsureLoadedAsync().ConfigureAwait(false);
        return _byId!.TryGetValue(id.Trim(), out var issuer) ? issuer : null;
    }

    public async Task<Issuer?> MatchAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        await EnsureLoadedAsync().ConfigureAwait(false);

        var needle = text.Trim();
        foreach (var issuer in _issuers!)
        {
            if (string.Equals(issuer.Name, needle, StringComparison.OrdinalIgnoreCase))
            {
                return issuer;
            }

            foreach (var alias in issuer.Aliases)
            {
                if (string.Equals(alias, needle, StringComparison.OrdinalIgnoreCase))
                {
                    return issuer;
                }
            }
        }

        return null;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_issuers is not null)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_issuers is not null)
            {
                return;
            }

            SeedFile seed;
            try
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync(SeedAsset).ConfigureAwait(false);
                seed = await JsonSerializer.DeserializeAsync<SeedFile>(stream, JsonOptions).ConfigureAwait(false)
                       ?? throw new InvalidOperationException($"Seed emittenti '{SeedAsset}' vuoto o non valido.");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Impossibile caricare il seed emittenti '{SeedAsset}'.", ex);
            }

            if (seed.Version != SupportedVersion)
            {
                throw new InvalidOperationException(
                    $"Versione del seed emittenti non supportata: {seed.Version} (attesa {SupportedVersion}).");
            }

            var issuers = seed.Issuers ?? new List<Issuer>();
            _byId = issuers
                .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                .ToDictionary(i => i.Id, StringComparer.Ordinal);
            _issuers = issuers;
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

        [JsonPropertyName("issuers")]
        public List<Issuer>? Issuers { get; set; }
    }
}
