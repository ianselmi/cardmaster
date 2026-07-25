using System.Text.Json.Serialization;

namespace CardMaster.Services.Backup;

// DTO di trasporto per gli endpoint OAuth e Drive v3. La (de)serializzazione usa il
// source-generator di System.Text.Json (nessuna reflection: robusto a trimming/AOT,
// coerente con CardShareCodec). Drive v3 restituisce i valori numerici a 64 bit come
// stringhe JSON (size/limit/usage), quindi qui sono tipizzati come string? e parsati a valle.

internal sealed class TokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("id_token")] public string? IdToken { get; set; }
    [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
}

internal sealed class AboutResponse
{
    [JsonPropertyName("storageQuota")] public StorageQuotaDto? StorageQuota { get; set; }
}

internal sealed class StorageQuotaDto
{
    [JsonPropertyName("limit")] public string? Limit { get; set; }
    [JsonPropertyName("usage")] public string? Usage { get; set; }
}

internal sealed class FileListResponse
{
    [JsonPropertyName("files")] public List<DriveFileDto>? Files { get; set; }
}

internal sealed class DriveFileDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("modifiedTime")] public DateTimeOffset? ModifiedTime { get; set; }
    [JsonPropertyName("size")] public string? Size { get; set; }
}

internal sealed class UploadMetadata
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("parents")] public string[]? Parents { get; set; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(AboutResponse))]
[JsonSerializable(typeof(FileListResponse))]
[JsonSerializable(typeof(DriveFileDto))]
[JsonSerializable(typeof(UploadMetadata))]
internal sealed partial class BackupJsonContext : JsonSerializerContext
{
}
