namespace CardMaster.Services.Backup;

/// <summary>Frequenza del backup automatico scelta dall'utente.</summary>
public enum BackupFrequency
{
    Never,
    OnEachOpen,
    Daily,
    Weekly,
}

/// <summary>Un backup presente nella cartella applicativa di Drive (metadati minimi).</summary>
public sealed record DriveBackupFile(string Id, string Name, DateTimeOffset ModifiedTime, long Size);

/// <summary>
/// Quota dello spazio Drive dell'account. <see cref="Limit"/> null = spazio illimitato
/// (l'account non espone un limite).
/// </summary>
public sealed record StorageQuota(long? Limit, long Usage);

/// <summary>Esito di un'operazione di backup manuale.</summary>
public sealed record BackupResult(bool Success, string? ErrorMessage = null, DriveBackupFile? File = null);

/// <summary>Esito di un ripristino.</summary>
public enum RestoreOutcome
{
    Success,
    SchemaTooNew,
    NotFound,
    Failed,
}

/// <summary>Risultato dettagliato di un ripristino.</summary>
public sealed record RestoreResult(RestoreOutcome Outcome, string? ErrorMessage = null);
