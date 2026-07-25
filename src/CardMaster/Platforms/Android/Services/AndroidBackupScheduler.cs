using Android.App;
using AndroidX.Work;
using CardMaster.Services.Backup;
using Java.Util.Concurrent;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Schedulazione dei backup periodici via Android WorkManager, con vincolo di rete connessa.
/// Solo Giornaliero/Settimanale producono un periodic work; Mai e "A ogni apertura" annullano
/// il job (quest'ultimo è gestito da un trigger d'avvio dell'app).
/// </summary>
public sealed class AndroidBackupScheduler : IBackupScheduler
{
    private const string WorkName = "cardmaster_backup_periodic";

    public void Schedule(BackupFrequency frequency)
    {
        long days = frequency switch
        {
            BackupFrequency.Daily => 1L,
            BackupFrequency.Weekly => 7L,
            _ => 0L,
        };

        if (days == 0L)
        {
            Cancel();
            return;
        }

        var constraints = new Constraints.Builder()
            .SetRequiredNetworkType(NetworkType.Connected!)
            .Build();

        var request = new PeriodicWorkRequest.Builder(
                Java.Lang.Class.FromType(typeof(BackupWorker)),
                days,
                TimeUnit.Days!)
            .SetConstraints(constraints!)
            .Build();

        WorkManager.GetInstance(global::Android.App.Application.Context)
            .EnqueueUniquePeriodicWork(WorkName, ExistingPeriodicWorkPolicy.Update!, request);
    }

    public void Cancel()
    {
        WorkManager.GetInstance(global::Android.App.Application.Context).CancelUniqueWork(WorkName);
    }
}
