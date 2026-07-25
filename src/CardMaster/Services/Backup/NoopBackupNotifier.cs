namespace CardMaster.Services.Backup;

/// <summary>Notifier no-op per le piattaforme senza notifiche (fallback non-Android).</summary>
public sealed class NoopBackupNotifier : IBackupNotifier
{
    public void NotifyInProgress()
    {
    }

    public void NotifyResult(bool success)
    {
    }
}
