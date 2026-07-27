using Android.Content;
using AndroidX.Work;
using CardMaster.Services.Update;
using Microsoft.Extensions.DependencyInjection;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Worker WorkManager per il controllo aggiornamenti ad app chiusa. Delega a
/// <see cref="IUpdateService.CheckForUpdateIfDueAsync"/>, che applica già l'opt-in e l'intervallo
/// minimo tra due controlli: qui non si duplica nessuna di quelle regole. La notifica di sistema,
/// se serve, la emette il servizio stesso.
/// </summary>
public sealed class UpdateCheckWorker : Worker
{
    public UpdateCheckWorker(Context context, WorkerParameters workerParameters)
        : base(context, workerParameters)
    {
    }

    public override Result DoWork()
    {
        try
        {
            var service = IPlatformApplication.Current?.Services.GetService<IUpdateService>();
            if (service is null)
            {
                return Result.InvokeSuccess();
            }

            // DoWork è sincrono e gira già su un thread di background di WorkManager.
            service.CheckForUpdateIfDueAsync().GetAwaiter().GetResult();
            return Result.InvokeSuccess();
        }
        catch (Java.Lang.Exception)
        {
            return Result.InvokeSuccess();
        }
        catch (System.Exception)
        {
            // Un controllo mancato non va ritentato con insistenza: il prossimo giro basta.
            // Restituire Retry rischierebbe backoff ripetuti per una rete che non c'è.
            return Result.InvokeSuccess();
        }
    }
}
