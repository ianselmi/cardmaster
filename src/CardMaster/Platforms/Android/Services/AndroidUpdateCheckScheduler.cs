using AndroidX.Work;
using CardMaster.Services.Update;
using Java.Util.Concurrent;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Controllo aggiornamenti periodico via Android WorkManager, con vincolo di rete connessa.
/// Stesso modello di <see cref="AndroidBackupScheduler"/>. Periodo <b>orario</b>: deve restare
/// allineato a <c>MinAutoCheckInterval</c> di <c>UpdateService</c>, che applica lo stesso minimo
/// tra due controlli — se il minimo fosse più lungo del periodo, il worker girerebbe a vuoto.
/// WorkManager non garantisce la puntualità: con Doze e batching l'esecuzione reale può slittare.
/// </summary>
public sealed class AndroidUpdateCheckScheduler : IUpdateCheckScheduler
{
    private const string WorkName = "cardmaster_update_check_periodic";

    public void Schedule()
    {
        var constraints = new Constraints.Builder()
            .SetRequiredNetworkType(NetworkType.Connected!)
            .Build();

        var request = new PeriodicWorkRequest.Builder(
                Java.Lang.Class.FromType(typeof(UpdateCheckWorker)),
                1L,
                TimeUnit.Hours!)
            .SetConstraints(constraints!)
            .Build();

        // Update: ri-registrare non duplica il lavoro né ne azzera il periodo in corso.
        WorkManager.GetInstance(global::Android.App.Application.Context)
            .EnqueueUniquePeriodicWork(WorkName, ExistingPeriodicWorkPolicy.Update!, request);
    }

    public void Cancel()
    {
        WorkManager.GetInstance(global::Android.App.Application.Context).CancelUniqueWork(WorkName);
    }
}
