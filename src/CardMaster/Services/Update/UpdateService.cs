using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace CardMaster.Services.Update;

/// <summary>
/// Implementazione di <see cref="IUpdateService"/>. Legge la Release GitHub con tag <c>latest</c>
/// (API pubbliche, nessuna autenticazione — repository pubblico), scarica l'APK in cache con
/// avanzamento riportato sia via <see cref="IUpdateNotifier"/> (notifica di sistema) sia via
/// <see cref="StateChanged"/> (UI in-app), e verifica lo SHA-256 quando l'asset lo espone. Gli
/// errori diventano esiti tipizzati, mai eccezioni verso i chiamanti.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private const string ReleaseUrl = "https://api.github.com/repos/ianselmi/cardmaster/releases/tags/latest";
    private const string DigestPrefix = "sha256:";
    private static readonly TimeSpan MinAutoCheckInterval = TimeSpan.FromHours(24);

    private readonly HttpClient _http;
    private readonly IUpdateNotifier _notifier;
    private readonly ISettingsStore _settings;

    public UpdateService(HttpClient http, IUpdateNotifier notifier, ISettingsStore settings)
    {
        _http = http;
        _notifier = notifier;
        _settings = settings;
    }

    public UpdateRelease? LastCheckedRelease { get; private set; }

    /// <summary>Versione installata, secondo la piattaforma. Confrontabile col nome della Release <c>latest</c>.</summary>
    private static string InstalledVersion => AppInfo.Current.VersionString;

    public string? AvailableUpdateVersion
    {
        get
        {
            // LastCheckedRelease vive quanto il processo; la preferenza sopravvive
            // all'aggiornamento dell'app, quindi va sempre confrontata con l'installato.
            var candidate = LastCheckedRelease?.VersionName ?? _settings.LastUpdateCheckAvailableVersion;
            return IsInstalled(candidate) ? null : candidate;
        }
    }

    public bool IsDownloading { get; private set; }

    public double DownloadProgress { get; private set; }

    public UpdateDownloadResult? LastDownloadResult { get; private set; }

    public event EventHandler? StateChanged;

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        GitHubReleaseDto? release;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleaseUrl);
            // Le API GitHub richiedono uno User-Agent anche per richieste anonime.
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CardMaster", AppInfo.Current.VersionString));

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new UpdateCheckResult(
                    UpdateCheckOutcome.Error,
                    ErrorMessage: "Nessuna release trovata. Riprova più tardi.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new UpdateCheckResult(
                    UpdateCheckOutcome.Error,
                    ErrorMessage: "Troppe richieste alle API di GitHub. Riprova tra qualche minuto.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(
                    UpdateCheckOutcome.Error,
                    ErrorMessage: $"Il server ha risposto {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            release = JsonSerializer.Deserialize(json, UpdateJsonContext.Default.GitHubReleaseDto);
        }
        catch (HttpRequestException)
        {
            return new UpdateCheckResult(UpdateCheckOutcome.Error, ErrorMessage: "Controllo aggiornamenti non riuscito per un errore di rete.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new UpdateCheckResult(UpdateCheckOutcome.Error, ErrorMessage: "Controllo aggiornamenti scaduto per timeout.");
        }
        catch (JsonException)
        {
            return new UpdateCheckResult(UpdateCheckOutcome.Error, ErrorMessage: "Risposta della release non valida.");
        }

        var apkAsset = release?.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) == true);
        if (release is null || string.IsNullOrEmpty(release.Name) || apkAsset?.BrowserDownloadUrl is null)
        {
            return new UpdateCheckResult(UpdateCheckOutcome.Error, ErrorMessage: "Release senza APK allegato.");
        }

        var sha256 = apkAsset.Digest?.StartsWith(DigestPrefix, StringComparison.OrdinalIgnoreCase) == true
            ? apkAsset.Digest[DigestPrefix.Length..]
            : null;

        var updateRelease = new UpdateRelease(release.Name, apkAsset.BrowserDownloadUrl, apkAsset.Size, sha256);

        _settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;

        // Confronto per uguaglianza (non ordinamento): il tag "latest" è sempre allineato
        // all'ultima build pubblicata, quindi "diverso" implica già "più recente" nel flusso normale.
        if (IsInstalled(updateRelease.VersionName))
        {
            LastCheckedRelease = null;
            _settings.LastUpdateCheckAvailableVersion = null;
            return new UpdateCheckResult(UpdateCheckOutcome.UpToDate);
        }

        LastCheckedRelease = updateRelease;
        _settings.LastUpdateCheckAvailableVersion = updateRelease.VersionName;
        return new UpdateCheckResult(UpdateCheckOutcome.UpdateAvailable, updateRelease);
    }

    public void ReconcileInstalledVersion()
    {
        var changed = false;

        if (IsInstalled(LastCheckedRelease?.VersionName))
        {
            LastCheckedRelease = null;
            changed = true;
        }

        if (IsInstalled(_settings.LastUpdateCheckAvailableVersion))
        {
            _settings.LastUpdateCheckAvailableVersion = null;
            changed = true;
        }

        // Il silenziamento si dimentica SOLO per la versione installata: quello di una versione
        // remota mai installata resta valido, è una scelta dell'utente ancora attuale.
        if (IsInstalled(_settings.UpdateNotifyDismissedVersion))
        {
            _settings.UpdateNotifyDismissedVersion = null;
            changed = true;
        }

        // LastUpdateCheckUtc resta intatto: un controllo è avvenuto davvero, ha solo perso
        // rilevanza il suo esito. Azzerarlo mostrerebbe "Nessun controllo ancora effettuato"
        // a chi l'ha appena fatto e farebbe ripartire subito il controllo automatico.

        if (changed)
        {
            RaiseStateChanged();
        }
    }

    /// <summary>Vero se la versione indicata è quella attualmente installata (aggiornamento già applicato).</summary>
    private static bool IsInstalled(string? version)
        => version is not null && string.Equals(version, InstalledVersion, StringComparison.Ordinal);

    public async Task CheckForUpdateIfDueAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.UpdateNotifyEnabled)
        {
            return;
        }

        if (_settings.LastUpdateCheckUtc is { } last && DateTimeOffset.UtcNow - last < MinAutoCheckInterval)
        {
            return;
        }

        await CheckForUpdateAsync(cancellationToken).ConfigureAwait(false);
        RaiseStateChanged();
    }

    public async Task<UpdateDownloadResult> DownloadAsync(CancellationToken cancellationToken = default)
    {
        if (IsDownloading)
        {
            return LastDownloadResult ?? new UpdateDownloadResult(UpdateDownloadOutcome.NetworkError, ErrorMessage: "Download già in corso.");
        }

        var release = LastCheckedRelease;
        if (release is null)
        {
            return new UpdateDownloadResult(UpdateDownloadOutcome.NetworkError, ErrorMessage: "Nessun aggiornamento da scaricare: avvia prima un controllo.");
        }

        IsDownloading = true;
        DownloadProgress = 0;
        LastDownloadResult = null;
        RaiseStateChanged();
        _notifier.NotifyProgress(0);

        var path = Path.Combine(FileSystem.CacheDirectory, $"update-{release.VersionName}.apk");
        TryDelete(path);

        try
        {
            using var response = await _http.GetAsync(release.ApkUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Complete(new UpdateDownloadResult(UpdateDownloadOutcome.NetworkError, ErrorMessage: $"Download non riuscito ({(int)response.StatusCode})."), path);
            }

            var totalBytes = response.Content.Headers.ContentLength ?? (release.ApkSize > 0 ? release.ApkSize : null);

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var destination = File.Create(path))
            {
                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    readTotal += read;

                    if (totalBytes is > 0)
                    {
                        ReportProgress((double)readTotal / totalBytes.Value);
                    }
                }
            }

            // Checksum best-effort: verificato solo se GitHub espone il digest per l'asset.
            // La garanzia primaria resta la firma verificata dal package installer di Android.
            if (release.Sha256 is not null)
            {
                var actualHash = await ComputeSha256Async(path, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(actualHash, release.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(path);
                    return Complete(new UpdateDownloadResult(UpdateDownloadOutcome.ChecksumMismatch, ErrorMessage: "Il file scaricato non corrisponde al checksum atteso."), null);
                }
            }

            return Complete(new UpdateDownloadResult(UpdateDownloadOutcome.Success, path), null);
        }
        catch (HttpRequestException)
        {
            return Complete(new UpdateDownloadResult(UpdateDownloadOutcome.NetworkError, ErrorMessage: "Download interrotto per un errore di rete."), path);
        }
        catch (IOException)
        {
            return Complete(new UpdateDownloadResult(UpdateDownloadOutcome.NetworkError, ErrorMessage: "Download interrotto per un errore di scrittura del file."), path);
        }
        catch (OperationCanceledException)
        {
            return Complete(new UpdateDownloadResult(UpdateDownloadOutcome.Cancelled), path);
        }
    }

    private void ReportProgress(double value)
    {
        DownloadProgress = Math.Clamp(value, 0, 1);
        _notifier.NotifyProgress(DownloadProgress);
        RaiseStateChanged();
    }

    private UpdateDownloadResult Complete(UpdateDownloadResult result, string? fileToDeleteOnFailure)
    {
        if (result.Outcome != UpdateDownloadOutcome.Success)
        {
            TryDelete(fileToDeleteOnFailure);
        }

        IsDownloading = false;
        DownloadProgress = result.Outcome == UpdateDownloadOutcome.Success ? 1 : 0;
        LastDownloadResult = result;
        _notifier.NotifyResult(result.Outcome == UpdateDownloadOutcome.Success);
        RaiseStateChanged();
        return result;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort: un file temporaneo residuo non compromette la funzione.
        }
    }
}
