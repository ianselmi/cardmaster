namespace CardMaster.Services.Update;

/// <summary>
/// Avvia il download dell'aggiornamento in corso in un contesto che sopravvive al passaggio in
/// background dell'app. Su Android avvia un foreground service; altrove esegue il download in-process.
/// </summary>
public interface IUpdateDownloadLauncher
{
    void StartDownload();
}
