namespace CardMaster.Services.Backup;

/// <summary>
/// Pianifica i backup periodici in background. Astrazione cross-platform: fuori da Android
/// l'implementazione è un no-op. Su Android usa WorkManager con vincolo di rete connessa.
/// La frequenza "A ogni apertura" non è schedulata qui (è un trigger d'avvio dell'app).
/// </summary>
public interface IBackupScheduler
{
    /// <summary>Pianifica (o ripianifica) il backup periodico per la frequenza indicata.</summary>
    void Schedule(BackupFrequency frequency);

    /// <summary>Annulla ogni backup periodico pianificato.</summary>
    void Cancel();
}
