namespace CardMaster.Services.Backup;

/// <summary>
/// Notifica di avanzamento del backup ("Backup in corso…") e di esito finale. Su Android è
/// implementata con canale notifiche + foreground service; altrove è un no-op.
/// </summary>
public interface IBackupNotifier
{
    /// <summary>Mostra la notifica "Backup in corso…".</summary>
    void NotifyInProgress();

    /// <summary>Aggiorna la notifica con l'esito finale (completato/fallito).</summary>
    void NotifyResult(bool success);
}
