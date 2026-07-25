namespace CardMaster.Services.Update;

/// <summary>
/// Notifica di avanzamento del download di un aggiornamento e di esito finale. Su Android è
/// implementata con canale notifiche a progresso determinato + foreground service; altrove è un no-op.
/// </summary>
public interface IUpdateNotifier
{
    /// <summary>Aggiorna la notifica "Download aggiornamento…" con la percentuale (0.0-1.0).</summary>
    void NotifyProgress(double progress);

    /// <summary>Aggiorna la notifica con l'esito finale del download (completato/fallito).</summary>
    void NotifyResult(bool success);
}
