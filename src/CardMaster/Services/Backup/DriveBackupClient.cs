using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CardMaster.Services.Backup;

/// <summary>
/// Implementazione di <see cref="IDriveBackupClient"/> su Drive v3 REST. Serializzazione
/// source-gen (no reflection). Retry singolo su 401 con refresh del token. Gli errori di
/// rete/servizio sono racchiusi in <see cref="DriveBackupException"/> (mai eccezioni grezze).
/// </summary>
public sealed class DriveBackupClient : IDriveBackupClient
{
    private const string ApiBase = "https://www.googleapis.com/drive/v3";
    private const string UploadBase = "https://www.googleapis.com/upload/drive/v3";
    private const string AppDataFolder = "appDataFolder";

    private readonly HttpClient _http;
    private readonly IGoogleAuth _auth;

    public DriveBackupClient(HttpClient http, IGoogleAuth auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<StorageQuota?> GetStorageQuotaAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/about?fields=storageQuota"), token),
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);

        // Lo scope appdata potrebbe non bastare per la quota: in tal caso degradiamo senza quota.
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var about = JsonSerializer.Deserialize(json, BackupJsonContext.Default.AboutResponse);
        if (about?.StorageQuota is null)
        {
            return null;
        }

        var usage = ParseLong(about.StorageQuota.Usage) ?? 0;
        var limit = ParseLong(about.StorageQuota.Limit); // assente = illimitato
        return new StorageQuota(limit, usage);
    }

    public async Task<IReadOnlyList<DriveBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{ApiBase}/files" +
                  $"?spaces={AppDataFolder}" +
                  "&fields=" + Uri.EscapeDataString("files(id,name,modifiedTime,size)") +
                  "&orderBy=" + Uri.EscapeDataString("modifiedTime desc") +
                  "&pageSize=100";

        using var response = await SendAsync(
            token => Authorized(new HttpRequestMessage(HttpMethod.Get, url), token),
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var list = JsonSerializer.Deserialize(json, BackupJsonContext.Default.FileListResponse);

        var files = new List<DriveBackupFile>();
        foreach (var dto in list?.Files ?? [])
        {
            if (string.IsNullOrEmpty(dto.Id) || string.IsNullOrEmpty(dto.Name))
            {
                continue;
            }

            files.Add(new DriveBackupFile(
                dto.Id,
                dto.Name,
                dto.ModifiedTime ?? DateTimeOffset.MinValue,
                ParseLong(dto.Size) ?? 0));
        }

        return files;
    }

    public async Task<DriveBackupFile> UploadBackupAsync(string name, byte[] content, CancellationToken cancellationToken = default)
    {
        var metadata = new UploadMetadata { Name = name, Parents = [AppDataFolder] };
        var metadataJson = JsonSerializer.Serialize(metadata, BackupJsonContext.Default.UploadMetadata);

        var url = $"{UploadBase}/files" +
                  "?uploadType=multipart" +
                  "&fields=" + Uri.EscapeDataString("id,name,modifiedTime,size");

        using var response = await SendAsync(
            token =>
            {
                var multipart = new MultipartContent("related", "cardmaster-boundary");
                var metaContent = new StringContent(metadataJson, Encoding.UTF8, "application/json");
                multipart.Add(metaContent);

                var mediaContent = new ByteArrayContent(content);
                mediaContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                multipart.Add(mediaContent);

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = multipart };
                return Authorized(request, token);
            },
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize(json, BackupJsonContext.Default.DriveFileDto);
        if (dto?.Id is null)
        {
            throw new DriveBackupException("Risposta di upload priva di id file.");
        }

        return new DriveBackupFile(
            dto.Id,
            dto.Name ?? name,
            dto.ModifiedTime ?? DateTimeOffset.UtcNow,
            ParseLong(dto.Size) ?? content.LongLength);
    }

    public async Task DownloadBackupAsync(string fileId, string destinationPath, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/files/{fileId}?alt=media"), token),
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteBackupAsync(string fileId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{ApiBase}/files/{fileId}"), token),
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);

        // 404 = già assente: idempotente, non è un errore.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static HttpRequestMessage Authorized(HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<string, HttpRequestMessage> build,
        HttpCompletionOption completion,
        CancellationToken cancellationToken)
    {
        var token = await _auth.GetValidAccessTokenAsync(false, cancellationToken).ConfigureAwait(false)
                    ?? throw new DriveBackupException("Nessun account Google collegato.") { RequiresReauth = true };

        HttpResponseMessage response;
        try
        {
            using var first = build(token);
            response = await _http.SendAsync(first, completion, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new DriveBackupException("Operazione Drive non riuscita per un errore di rete.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DriveBackupException("Operazione Drive scaduta.", ex);
        }

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // 401: rinnova il token e ritenta una volta sola.
        response.Dispose();
        var refreshed = await _auth.GetValidAccessTokenAsync(true, cancellationToken).ConfigureAwait(false)
                        ?? throw new DriveBackupException("Credenziali Google scadute o revocate.") { RequiresReauth = true };

        try
        {
            using var retry = build(refreshed);
            response = await _http.SendAsync(retry, completion, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new DriveBackupException("Operazione Drive non riuscita per un errore di rete.", ex);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            throw new DriveBackupException("Credenziali Google scadute o revocate.") { RequiresReauth = true };
        }

        return response;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new DriveBackupException($"Drive ha risposto {(int)response.StatusCode}: {Truncate(body)}");
    }

    private static string Truncate(string text) =>
        text.Length <= 200 ? text : text[..200];

    private static long? ParseLong(string? value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
}
