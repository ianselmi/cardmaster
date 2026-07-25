using System.Text.Json.Serialization;

namespace CardMaster.Services.Update;

// DTO della risposta dell'API GitHub Releases (GET /repos/{owner}/{repo}/releases/tags/{tag}).
// Serializzazione source-gen (nessuna reflection), coerente con BackupJsonContext.

internal sealed class GitHubReleaseDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("assets")] public List<GitHubReleaseAssetDto>? Assets { get; set; }
}

internal sealed class GitHubReleaseAssetDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }

    // Formato "sha256:<hex>", presente solo per gli asset caricati dopo l'introduzione del campo
    // da parte di GitHub: trattato come best-effort, non garantito.
    [JsonPropertyName("digest")] public string? Digest { get; set; }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GitHubReleaseDto))]
internal sealed partial class UpdateJsonContext : JsonSerializerContext
{
}
