namespace CardMaster.Services.Update;

/// <summary>Notifier no-op per le piattaforme senza notifiche (fallback non-Android).</summary>
public sealed class NoopUpdateNotifier : IUpdateNotifier
{
    public void NotifyProgress(double progress)
    {
    }

    public void NotifyResult(bool success)
    {
    }
}
