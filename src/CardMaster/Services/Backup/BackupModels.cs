namespace CardMaster.Services.Backup;

/// <summary>Frequenza del backup automatico scelta dall'utente.</summary>
public enum BackupFrequency
{
    Never,
    OnEachOpen,
    Daily,
    Weekly,
}

/// <summary>
/// Categoria di un fallimento di backup/ripristino. È la sola informazione su cui la UI
/// costruisce il messaggio mostrato all'utente: il testo tecnico del servizio resta un
/// dettaglio diagnostico interno.
/// </summary>
public enum BackupErrorKind
{
    /// <summary>Nessun errore (ultimo tentativo riuscito o mai eseguito).</summary>
    None,

    /// <summary>Rete assente o non raggiungibile.</summary>
    Network,

    /// <summary>Spazio dell'account Google Drive esaurito.</summary>
    StorageFull,

    /// <summary>Credenziali Google non più valide: serve riconnettere l'account.</summary>
    ReauthRequired,

    /// <summary>Errore del servizio Drive (5xx, rate limit, risposta inattesa).</summary>
    Service,

    /// <summary>Errore locale al device (snapshot o sostituzione del database).</summary>
    Local,
}

/// <summary>Un backup presente nella cartella applicativa di Drive (metadati minimi).</summary>
public sealed record DriveBackupFile(string Id, string Name, DateTimeOffset ModifiedTime, long Size);

/// <summary>
/// Quota dello spazio Drive dell'account. <see cref="Limit"/> null = spazio illimitato
/// (l'account non espone un limite).
/// </summary>
public sealed record StorageQuota(long? Limit, long Usage);

/// <summary>
/// Esito di un'operazione di backup. <see cref="Kind"/> guida il messaggio mostrato all'utente;
/// <see cref="ErrorMessage"/> è il dettaglio tecnico, da non mostrare in primo piano.
/// </summary>
public sealed record BackupResult(
    bool Success,
    string? ErrorMessage = null,
    DriveBackupFile? File = null,
    BackupErrorKind Kind = BackupErrorKind.None);

/// <summary>Esito di un ripristino.</summary>
public enum RestoreOutcome
{
    Success,
    SchemaTooNew,
    NotFound,
    Failed,
}

/// <summary>
/// Risultato dettagliato di un ripristino. Come per il backup, <see cref="Kind"/> è ciò che
/// determina il messaggio mostrato all'utente.
/// </summary>
public sealed record RestoreResult(
    RestoreOutcome Outcome,
    string? ErrorMessage = null,
    BackupErrorKind Kind = BackupErrorKind.None);
