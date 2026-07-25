using Android.App;
using Android.Content;
using Android.OS;
using CardMaster.Services.Update;
using Microsoft.Extensions.DependencyInjection;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Foreground service (type dataSync) che avvolge il download di un aggiornamento: mostra la
/// notifica "Download aggiornamento… N%" e garantisce l'esecuzione anche se l'app va in
/// background. L'avanzamento in-app è alimentato dall'evento <see cref="IUpdateService.StateChanged"/>
/// del servizio condiviso (singleton), non da questo service.
/// </summary>
[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
public sealed class UpdateDownloadForegroundService : Service
{
    public static void Start(Context context)
    {
        var intent = new Intent(context, typeof(UpdateDownloadForegroundService));
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
        var notification = AndroidUpdateNotifier.BuildProgressNotification(this, 0);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(
                AndroidUpdateNotifier.ProgressNotificationId,
                notification,
                global::Android.Content.PM.ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(AndroidUpdateNotifier.ProgressNotificationId, notification);
        }

        _ = RunAsync();
        return StartCommandResult.NotSticky;
    }

    private async Task RunAsync()
    {
        try
        {
            var service = IPlatformApplication.Current?.Services.GetService<IUpdateService>();
            if (service is not null)
            {
                await service.DownloadAsync().ConfigureAwait(false);
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
