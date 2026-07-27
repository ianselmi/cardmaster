namespace CardMaster.Services.Update;

/// <summary>
/// Pianificazione del controllo aggiornamenti periodico ad app chiusa. Su Android è implementata
/// con WorkManager (come i backup periodici); altrove è un no-op. Il rispetto dell'intervallo
/// minimo tra due controlli NON è compito dello scheduler: resta di
/// <see cref="IUpdateService.CheckForUpdateIfDueAsync"/>, così la regola vive in un solo posto.
/// </summary>
public interface IUpdateCheckScheduler
{
    /// <summary>Registra il controllo periodico. Idempotente: ri-registrare non duplica il lavoro.</summary>
    void Schedule();

    /// <summary>Annulla il controllo periodico: dopo questa chiamata nessuna richiesta di rete automatica.</summary>
    void Cancel();
}
