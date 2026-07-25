using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using CardMaster.Services.Update;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Notifiche di avanzamento/esito del download di un aggiornamento su Android. Canale a bassa
/// priorità con progresso determinato (percentuale nota dal Content-Length della risposta HTTP),
/// a differenza del backup che è indeterminato. La stessa notifica di avanzamento alimenta il
/// foreground service (vedi <see cref="UpdateDownloadForegroundService"/>).
/// </summary>
public sealed class AndroidUpdateNotifier : IUpdateNotifier
{
    public const string ChannelId = "cardmaster_update";
    public const int ProgressNotificationId = 4301;

    public AndroidUpdateNotifier()
    {
        EnsureChannel(global::Android.App.Application.Context);
    }

    public void NotifyProgress(double progress)
    {
        var percent = (int)Math.Round(Math.Clamp(progress, 0, 1) * 100);
        var notification = BuildProgressNotification(global::Android.App.Application.Context, percent);
        NotificationManagerCompat.From(global::Android.App.Application.Context).Notify(ProgressNotificationId, notification);
    }

    public void NotifyResult(bool success)
    {
        var context = global::Android.App.Application.Context;
        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(success
                ? global::Android.Resource.Drawable.StatSysDownloadDone
                : global::Android.Resource.Drawable.StatNotifyError)
            .SetContentTitle(success ? "Aggiornamento scaricato" : "Aggiornamento non riuscito")
            .SetContentText(success
                ? "Tocca \"Scarica e installa\" nell'app per completare l'installazione."
                : "Impossibile completare il download. Riprova dalle Impostazioni.")
            .SetAutoCancel(true)
            .SetOngoing(false)
            .SetPriority(NotificationCompat.PriorityLow);

        NotificationManagerCompat.From(context).Notify(ProgressNotificationId, builder.Build());
    }

    /// <summary>Costruisce la notifica "Download aggiornamento… N%" (usata anche dal foreground service).</summary>
    public static Notification BuildProgressNotification(Context context, int percent)
    {
        EnsureChannel(context);
        return new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload)
            .SetContentTitle("Download aggiornamento…")
            .SetContentText($"{percent}%")
            .SetOngoing(true)
            .SetProgress(100, percent, false)
            .SetPriority(NotificationCompat.PriorityLow)
            .Build();
    }

    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null || manager.GetNotificationChannel(ChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, "Aggiornamenti", NotificationImportance.Low)
        {
            Description = "Avanzamento ed esito del download degli aggiornamenti dell'app.",
        };
        manager.CreateNotificationChannel(channel);
    }
}
