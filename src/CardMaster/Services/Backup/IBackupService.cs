namespace CardMaster.Services.Backup;

/// <summary>
/// Orchestrazione del backup/ripristino su Google Drive: abilitazione (autenticazione +
/// schedulazione), esecuzione dello snapshot+upload con ritenzione, lista e ripristino
/// preceduto da un backup della situazione corrente, gestione dello stato locale. Un solo
/// account collegato alla volta (il cambio account passa da <see cref="DisableAsync"/> +
/// <see cref="EnableAsync"/>).
/// </summary>
public interface IBackupService
{
    bool IsEnabled { get; }

    string? AccountEmail { get; }

    BackupFrequency Frequency { get; }

    DateTimeOffset? LastBackupUtc { get; }

    long? LastBackupSize { get; }

    /// <summary>Categoria dell'errore dell'ultimo tentativo di backup; None se è andato a buon fine.</summary>
    BackupErrorKind LastError { get; }

    /// <summary>Quando è avvenuto l'ultimo tentativo di backup, riuscito o fallito; null se mai tentato.</summary>
    DateTimeOffset? LastAttemptUtc { get; }

    /// <summary>Quota cache locale (per la UI offline); null se mai letta.</summary>
    StorageQuota? CachedQuota { get; }

    /// <summary>Autentica l'account Google e abilita il backup. False se annullato/senza rete.</summary>
    Task<bool> EnableAsync(CancellationToken cancellationToken = default);

    /// <summary>Disabilita il backup: revoca credenziali e annulla la schedulazione.</summary>
    Task DisableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ripete il consenso Google dopo credenziali scadute/revocate, mantenendo abilitazione,
    /// frequenza e backup già presenti su Drive. False se l'utente annulla o manca la rete.
    /// </summary>
    Task<bool> ReconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Imposta la frequenza e ri-pianifica coerentemente.</summary>
    Task SetFrequencyAsync(BackupFrequency frequency);

    /// <summary>Esegue subito uno snapshot + upload con ritenzione (≤3).</summary>
    Task<BackupResult> BackupNowAsync(CancellationToken cancellationToken = default);

    /// <summary>Esegue un backup schedulato/di sistema (no-op se disabilitato).</summary>
    Task RunScheduledBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>Trigger "A ogni apertura": esegue un backup se abilitato, con quella frequenza e rete disponibile.</summary>
    Task MaybeBackupOnOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Elenco in-app dei backup disponibili su Drive.</summary>
    Task<IReadOnlyList<DriveBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ripristina il backup indicato sostituendo l'intero database. La situazione corrente viene
    /// prima salvata su Drive come backup ordinario: se quel backup non riesce il ripristino non
    /// viene eseguito (<see cref="RestoreOutcome.PreBackupFailed"/>) e il database resta intatto.
    /// </summary>
    Task<RestoreResult> RestoreAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>Rilegge la quota da Drive e ne aggiorna la cache locale. Null se non disponibile.</summary>
    Task<StorageQuota?> RefreshQuotaAsync(CancellationToken cancellationToken = default);
}
