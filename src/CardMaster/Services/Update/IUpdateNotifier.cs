namespace CardMaster.Services.Update;

/// <summary>
/// Notifiche di sistema legate agli aggiornamenti: disponibilità di una nuova versione,
/// avanzamento del download ed esito finale. Su Android è implementata con canale notifiche
/// (+ foreground service per il download); altrove è un no-op.
/// </summary>
public interface IUpdateNotifier
{
    /// <summary>Aggiorna la notifica "Download aggiornamento…" con la percentuale (0.0-1.0).</summary>
    void NotifyProgress(double progress);

    /// <summary>Aggiorna la notifica con l'esito finale del download (completato/fallito).</summary>
    void NotifyResult(bool success);

    /// <summary>
    /// Segnala che è disponibile la versione indicata. Notifica distinta da quella di download
    /// (id diverso): le due devono poter coesistere. Toccarla apre il flusso di aggiornamento.
    /// </summary>
    void NotifyUpdateAvailable(string version);

    /// <summary>Rimuove la notifica di disponibilità (aggiornamento installato, silenziato o opzione disattivata).</summary>
    void CancelUpdateAvailable();
}
