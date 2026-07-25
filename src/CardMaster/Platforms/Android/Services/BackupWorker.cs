using Android.Content;
using AndroidX.Work;
using CardMaster.Services.Backup;
using Microsoft.Extensions.DependencyInjection;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Worker WorkManager per i backup periodici (Giornaliero/Settimanale). Avvia il foreground
/// service per la notifica di avanzamento e delega a <see cref="IBackupService"/>.
/// </summary>
public sealed class BackupWorker : Worker
{
    public BackupWorker(Context context, WorkerParameters workerParameters)
        : base(context, workerParameters)
    {
    }

    public override Result DoWork()
    {
        try
        {
            var service = IPlatformApplication.Current?.Services.GetService<IBackupService>();
            if (service is null)
            {
                return Result.InvokeSuccess();
            }

            // Notifica di avanzamento via foreground service; l'upload effettivo è nel service.
            BackupForegroundService.Start(ApplicationContext);
            return Result.InvokeSuccess();
        }
        catch (Java.Lang.Exception)
        {
            return Result.InvokeRetry();
        }
    }
}
