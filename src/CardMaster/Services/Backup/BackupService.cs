using CardMaster.Services;

namespace CardMaster.Services.Backup;

/// <summary>
/// Implementazione di <see cref="IBackupService"/>. Coordina auth, Drive client, database e
/// scheduler; non lancia mai eccezioni non controllate verso la UI (gli errori diventano
/// esiti). Lo snapshot di sicurezza pre-ripristino resta in cache per un undo immediato.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IGoogleAuth _auth;
    private readonly IDriveBackupClient _drive;
    private readonly IDatabaseService _database;
    private readonly ISettingsStore _settings;
    private readonly IBackupScheduler _scheduler;
    private readonly IBackupNotifier _notifier;
    private readonly IBackupEnvironment _environment;

    private string? _safetySnapshotPath;

    public BackupService(
        IGoogleAuth auth,
        IDriveBackupClient drive,
        IDatabaseService database,
        ISettingsStore settings,
        IBackupScheduler scheduler,
        IBackupNotifier notifier,
        IBackupEnvironment environment)
    {
        _auth = auth;
        _drive = drive;
        _database = database;
        _settings = settings;
        _scheduler = scheduler;
        _notifier = notifier;
        _environment = environment;
    }

    public bool IsEnabled => _settings.BackupEnabled && _auth.IsSignedIn;

    public string? AccountEmail => _auth.AccountEmail;

    public BackupFrequency Frequency => _settings.BackupFrequency;

    public DateTimeOffset? LastBackupUtc => _settings.LastBackupUtc;

    public long? LastBackupSize => _settings.LastBackupSize;

    public BackupErrorKind LastError => _settings.LastBackupError;

    public DateTimeOffset? LastAttemptUtc => _settings.LastBackupAttemptUtc;

    public StorageQuota? CachedQuota =>
        _settings.DriveQuotaUsage is { } usage
            ? new StorageQuota(_settings.DriveQuotaLimit, usage)
            : null;

    public async Task<bool> EnableAsync(CancellationToken cancellationToken = default)
    {
        var ok = await _auth.SignInAsync(cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return false;
        }

        _settings.BackupEnabled = true;
        if (_settings.BackupFrequency == BackupFrequency.Never)
        {
            _settings.BackupFrequency = BackupFrequency.Daily;
        }

        ApplySchedule(_settings.BackupFrequency);
        return true;
    }

    public async Task DisableAsync(CancellationToken cancellationToken = default)
    {
        _scheduler.Cancel();
        await _auth.SignOutAsync(cancellationToken).ConfigureAwait(false);

        _settings.BackupEnabled = false;
        _settings.BackupFrequency = BackupFrequency.Never;
        _settings.LastBackupUtc = null;
        _settings.LastBackupSize = null;
        _settings.LastBackupAttemptUtc = null;
        _settings.LastBackupError = BackupErrorKind.None;
        _settings.DriveQuotaLimit = null;
        _settings.DriveQuotaUsage = null;
    }

    public async Task<bool> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        // Solo SignInAsync: il flusso è già prompt=consent + access_type=offline, quindi
        // sovrascrive il refresh token morto lasciando intatti abilitazione, frequenza,
        // schedulazione e backup su Drive. Passare da DisableAsync azzererebbe tutto.
        var ok = await _auth.SignInAsync(cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return false;
        }

        _settings.LastBackupError = BackupErrorKind.None;
        return true;
    }

    public Task SetFrequencyAsync(BackupFrequency frequency)
    {
        _settings.BackupFrequency = frequency;
        ApplySchedule(frequency);
        return Task.CompletedTask;
    }

    public async Task<BackupResult> BackupNowAsync(CancellationToken cancellationToken = default)
    {
        _notifier.NotifyInProgress();

        string? snapshotPath = null;
        try
        {
            snapshotPath = Path.Combine(_environment.CacheDirectory, $"backup-{Guid.NewGuid():N}.db3");
            await _database.SnapshotAsync(snapshotPath).ConfigureAwait(false);

            var name = BackupNaming.Build(DateTimeOffset.UtcNow, _database.CurrentSchemaVersion);
            var bytes = await File.ReadAllBytesAsync(snapshotPath, cancellationToken).ConfigureAwait(false);
            var uploaded = await _drive.UploadBackupAsync(name, bytes, cancellationToken).ConfigureAwait(false);

            await ApplyRetentionAsync(cancellationToken).ConfigureAwait(false);

            _settings.LastBackupUtc = uploaded.ModifiedTime;
            _settings.LastBackupSize = uploaded.Size;
            _settings.LastBackupAttemptUtc = DateTimeOffset.UtcNow;
            _settings.LastBackupError = BackupErrorKind.None;
            await RefreshQuotaAsync(cancellationToken).ConfigureAwait(false);

            _notifier.NotifyResult(true);
            return new BackupResult(true, File: uploaded);
        }
        catch (DriveBackupException ex)
        {
            return RecordFailure(ex.Kind, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            // Snapshot o lettura del file locale: il problema è sul device, non su Drive.
            return RecordFailure(BackupErrorKind.Local, ex.Message);
        }
        finally
        {
            TryDelete(snapshotPath);
        }
    }

    /// <summary>
    /// Registra un tentativo fallito. È l'unico punto in cui lo stato di errore viene scritto per
    /// i backup, quindi vale identico per manuale, "a ogni apertura" e schedulato in background.
    /// <see cref="ISettingsStore.LastBackupUtc"/> resta all'ultimo successo: è la coppia
    /// "ultimo backup riuscito" + "ultimo tentativo fallito" a smentire l'idea che tutto funzioni.
    /// </summary>
    private BackupResult RecordFailure(BackupErrorKind kind, string? message)
    {
        _settings.LastBackupAttemptUtc = DateTimeOffset.UtcNow;
        _settings.LastBackupError = kind;
        _notifier.NotifyResult(false);
        return new BackupResult(false, message, Kind: kind);
    }

    public async Task RunScheduledBackupAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await BackupNowAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MaybeBackupOnOpenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled
            || _settings.BackupFrequency != BackupFrequency.OnEachOpen
            || !_environment.HasNetworkAccess)
        {
            return;
        }

        await BackupNowAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DriveBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var files = await _drive.ListBackupsAsync(cancellationToken).ConfigureAwait(false);
        // Mostra solo i backup gestiti da questa app (nome conforme alla convenzione).
        return files.Where(f => BackupNaming.TryParse(f.Name, out _, out _)).ToList();
    }

    public async Task<RestoreResult> RestoreAsync(string backupId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DriveBackupFile> files;
        try
        {
            files = await _drive.ListBackupsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DriveBackupException ex)
        {
            return RestoreFailure(ex);
        }

        var target = files.FirstOrDefault(f => f.Id == backupId);
        if (target is null)
        {
            return new RestoreResult(RestoreOutcome.NotFound);
        }

        // Guardia di versione: no downgrade da un backup con schema più recente.
        if (BackupNaming.TryParse(target.Name, out _, out var backupSchema)
            && !BackupNaming.CanRestore(backupSchema, _database.CurrentSchemaVersion))
        {
            return new RestoreResult(RestoreOutcome.SchemaTooNew);
        }

        var downloadPath = Path.Combine(_environment.CacheDirectory, $"restore-{Guid.NewGuid():N}.db3");
        try
        {
            await _drive.DownloadBackupAsync(target.Id, downloadPath, cancellationToken).ConfigureAwait(false);
        }
        catch (DriveBackupException ex)
        {
            TryDelete(downloadPath);
            return RestoreFailure(ex);
        }

        // Snapshot di sicurezza del DB corrente prima di sostituirlo (per undo immediato).
        var safetyPath = Path.Combine(_environment.CacheDirectory, $"safety-{Guid.NewGuid():N}.db3");
        try
        {
            await _database.SnapshotAsync(safetyPath).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            TryDelete(downloadPath);
            TryDelete(safetyPath);
            return new RestoreResult(RestoreOutcome.Failed, ex.Message, BackupErrorKind.Local);
        }

        try
        {
            await _database.ReplaceFromAsync(downloadPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Swap fallito a metà: ripristina lo stato precedente dallo snapshot di sicurezza.
            try
            {
                await _database.ReplaceFromAsync(safetyPath).ConfigureAwait(false);
            }
            catch (Exception undoEx) when (undoEx is IOException or InvalidOperationException)
            {
                // Nulla di più da fare in automatico; lo snapshot resta in cache.
            }

            TryDelete(downloadPath);
            return new RestoreResult(RestoreOutcome.Failed, ex.Message, BackupErrorKind.Local);
        }

        // Conserva lo snapshot di sicurezza per un eventuale undo esplicito dell'utente.
        TryDelete(_safetySnapshotPath);
        _safetySnapshotPath = safetyPath;
        TryDelete(downloadPath);
        return new RestoreResult(RestoreOutcome.Success);
    }

    public async Task<bool> UndoLastRestoreAsync(CancellationToken cancellationToken = default)
    {
        if (_safetySnapshotPath is null || !File.Exists(_safetySnapshotPath))
        {
            return false;
        }

        try
        {
            await _database.ReplaceFromAsync(_safetySnapshotPath).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return false;
        }

        TryDelete(_safetySnapshotPath);
        _safetySnapshotPath = null;
        return true;
    }

    public async Task<StorageQuota?> RefreshQuotaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var quota = await _drive.GetStorageQuotaAsync(cancellationToken).ConfigureAwait(false);
            if (quota is not null)
            {
                _settings.DriveQuotaLimit = quota.Limit;
                _settings.DriveQuotaUsage = quota.Usage;
            }

            return quota;
        }
        catch (DriveBackupException ex)
        {
            // Un token revocato emerge già qui, all'apertura della pagina, senza attendere il
            // prossimo backup. Le altre categorie restano ignorate: una quota non aggiornata per
            // mancanza di rete non è un guasto del backup. LastBackupAttemptUtc non si tocca:
            // questa non è l'esecuzione di un backup.
            if (ex.Kind == BackupErrorKind.ReauthRequired)
            {
                _settings.LastBackupError = BackupErrorKind.ReauthRequired;
            }

            return CachedQuota;
        }
    }

    /// <summary>
    /// Un ripristino fallito non è un backup fallito: non tocca l'esito dell'ultimo tentativo.
    /// Fa eccezione il caso credenziali, che riguarda l'account e non la singola operazione.
    /// </summary>
    private RestoreResult RestoreFailure(DriveBackupException ex)
    {
        if (ex.Kind == BackupErrorKind.ReauthRequired)
        {
            _settings.LastBackupError = BackupErrorKind.ReauthRequired;
        }

        return new RestoreResult(RestoreOutcome.Failed, ex.Message, ex.Kind);
    }

    private async Task ApplyRetentionAsync(CancellationToken cancellationToken)
    {
        var files = await _drive.ListBackupsAsync(cancellationToken).ConfigureAwait(false);
        var managed = files.Where(f => BackupNaming.TryParse(f.Name, out _, out _));
        foreach (var excess in BackupNaming.SelectExcess(managed, BackupNaming.MaxRetained))
        {
            await _drive.DeleteBackupAsync(excess.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private void ApplySchedule(BackupFrequency frequency)
    {
        if (!_settings.BackupEnabled || frequency is BackupFrequency.Never or BackupFrequency.OnEachOpen)
        {
            _scheduler.Cancel();
        }
        else
        {
            _scheduler.Schedule(frequency);
        }
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
