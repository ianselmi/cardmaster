namespace CardMaster.Services.Backup;

/// <summary>Scheduler no-op per le piattaforme senza background job (fallback non-Android).</summary>
public sealed class NoopBackupScheduler : IBackupScheduler
{
    public void Schedule(BackupFrequency frequency)
    {
    }

    public void Cancel()
    {
    }
}
