using Android.App;
using Android.Content;
using Android.OS;
using CardMaster.Services.Backup;
using Microsoft.Extensions.DependencyInjection;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Foreground service (type dataSync) che avvolge l'esecuzione di un backup schedulato:
/// mostra la notifica "Backup in corso…" e garantisce l'esecuzione del job in background.
/// </summary>
[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
public sealed class BackupForegroundService : Service
{
    public static void Start(Context context)
    {
        var intent = new Intent(context, typeof(BackupForegroundService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var notification = AndroidBackupNotifier.BuildProgressNotification(this);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(
                AndroidBackupNotifier.ProgressNotificationId,
                notification,
                global::Android.Content.PM.ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(AndroidBackupNotifier.ProgressNotificationId, notification);
        }

        _ = RunAsync();
        return StartCommandResult.NotSticky;
    }

    private async Task RunAsync()
    {
        try
        {
            var service = IPlatformApplication.Current?.Services.GetService<IBackupService>();
            if (service is not null)
            {
                await service.RunScheduledBackupAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
#pragma warning disable CA1422 // API obsoleta solo su versioni < N
                StopForeground(true);
#pragma warning restore CA1422
            }

            StopSelf();
        }
    }
}
