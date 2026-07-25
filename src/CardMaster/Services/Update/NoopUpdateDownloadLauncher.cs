namespace CardMaster.Services.Update;

/// <summary>Launcher no-op per le piattaforme senza foreground service Android: esegue il download in-process.</summary>
public sealed class NoopUpdateDownloadLauncher : IUpdateDownloadLauncher
{
    private readonly IUpdateService _updateService;

    public NoopUpdateDownloadLauncher(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    public void StartDownload()
    {
        _ = _updateService.DownloadAsync();
    }
}
