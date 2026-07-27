namespace CardMaster.Services.Update;

/// <summary>Scheduler no-op per le piattaforme senza lavoro periodico (fallback non-Android).</summary>
public sealed class NoopUpdateCheckScheduler : IUpdateCheckScheduler
{
    public void Schedule()
    {
    }

    public void Cancel()
    {
    }
}
