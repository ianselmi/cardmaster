using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using CardMaster.Services.Backup;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Notifiche di avanzamento/esito del backup su Android. Crea un canale a bassa priorità e
/// riusa lo stesso id di notifica per aggiornare "in corso" → "completato/fallito".
/// La stessa notifica di avanzamento alimenta il foreground service (vedi
/// <see cref="BackupForegroundService"/>).
/// </summary>
public sealed class AndroidBackupNotifier : IBackupNotifier
{
    public const string ChannelId = "cardmaster_backup";
    public const int ProgressNotificationId = 4201;

    public AndroidBackupNotifier()
    {
        EnsureChannel(global::Android.App.Application.Context);
    }

    public void NotifyInProgress()
    {
        var notification = BuildProgressNotification(global::Android.App.Application.Context);
        NotificationManagerCompat.From(global::Android.App.Application.Context).Notify(ProgressNotificationId, notification);
    }

    public void NotifyResult(bool success)
    {
        var context = global::Android.App.Application.Context;
        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(success
                ? global::Android.Resource.Drawable.StatSysUploadDone
                : global::Android.Resource.Drawable.StatNotifyError)
            .SetContentTitle(success ? "Backup completato" : "Backup non riuscito")
            .SetContentText(success
                ? "Il backup su Google Drive è stato caricato."
                : "Impossibile completare il backup. Riprova.")
            .SetAutoCancel(true)
            .SetOngoing(false)
            .SetPriority(NotificationCompat.PriorityLow);

        NotificationManagerCompat.From(context).Notify(ProgressNotificationId, builder.Build());
    }

    /// <summary>Costruisce la notifica "Backup in corso…" (usata anche dal foreground service).</summary>
    public static Notification BuildProgressNotification(Context context)
    {
        EnsureChannel(context);
        return new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysUpload)
            .SetContentTitle("Backup in corso…")
            .SetContentText("Caricamento del backup su Google Drive.")
            .SetOngoing(true)
            .SetProgress(0, 0, true)
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

        var channel = new NotificationChannel(ChannelId, "Backup", NotificationImportance.Low)
        {
            Description = "Avanzamento ed esito del backup su Google Drive.",
        };
        manager.CreateNotificationChannel(channel);
    }
}
