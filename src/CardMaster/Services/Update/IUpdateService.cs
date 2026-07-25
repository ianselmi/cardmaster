namespace CardMaster.Services.Update;

/// <summary>
/// Controllo/download degli aggiornamenti dell'app: legge il manifest statico pubblicato dalla
/// pipeline CI su GitHub Pages, scarica e verifica l'APK. Nessuna autenticazione richiesta (il
/// repository sorgente resta privato; solo il manifest e l'APK pubblicati sono raggiungibili
/// pubblicamente). Nessun controllo automatico: sempre su azione esplicita dell'utente.
/// </summary>
public interface IUpdateService
{
    /// <summary>Versione rilevata come disponibile dall'ultimo <see cref="CheckForUpdateAsync"/> riuscito in questa sessione; null se nessuna o non ancora controllato.</summary>
    UpdateRelease? LastCheckedRelease { get; }

    /// <summary>True mentre un download è in corso.</summary>
    bool IsDownloading { get; }

    /// <summary>Avanzamento del download in corso, 0.0-1.0.</summary>
    double DownloadProgress { get; }

    /// <summary>Esito dell'ultimo download completato in questa sessione; null se nessuno ancora avvenuto.</summary>
    UpdateDownloadResult? LastDownloadResult { get; }

    /// <summary>Sollevato a ogni variazione di stato del download (avanzamento, completamento).</summary>
    event EventHandler? StateChanged;

    /// <summary>Interroga il manifest remoto e confronta la versione con quella installata.</summary>
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue <see cref="CheckForUpdateAsync"/> solo se l'utente ha attivato "Avvisami di nuove
    /// versioni" e sono trascorse almeno 24 ore dall'ultimo controllo; no-op altrimenti. Pensato per
    /// essere chiamato a ogni passaggio dell'app in foreground, senza mai interrogare la rete se
    /// l'opzione non è attiva.
    /// </summary>
    Task CheckForUpdateIfDueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scarica e verifica (checksum SHA-256) l'APK indicato da <see cref="LastCheckedRelease"/>.
    /// No-op (restituisce l'ultimo esito) se un download è già in corso.
    /// </summary>
    Task<UpdateDownloadResult> DownloadAsync(CancellationToken cancellationToken = default);
}
