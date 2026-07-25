using CardMaster.Services.Update;

namespace CardMaster.Platforms.Android.Services;

/// <summary>Avvia il download dell'aggiornamento tramite <see cref="UpdateDownloadForegroundService"/>.</summary>
public sealed class AndroidUpdateDownloadLauncher : IUpdateDownloadLauncher
{
    public void StartDownload() => UpdateDownloadForegroundService.Start(global::Android.App.Application.Context);
}
