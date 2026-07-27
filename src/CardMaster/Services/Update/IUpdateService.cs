namespace CardMaster.Services.Update;

/// <summary>
/// Controllo/download degli aggiornamenti dell'app: legge la Release GitHub con tag <c>latest</c>
/// (API pubbliche, repository pubblico), scarica e verifica l'APK. Il controllo avviene su azione
/// esplicita dell'utente, oppure automaticamente se è attiva l'opzione "Avvisami di nuove versioni"
/// con le limitazioni di `app-update-notify`.
/// </summary>
public interface IUpdateService
{
    /// <summary>Versione rilevata come disponibile dall'ultimo <see cref="CheckForUpdateAsync"/> riuscito in questa sessione; null se nessuna o non ancora controllato.</summary>
    UpdateRelease? LastCheckedRelease { get; }

    /// <summary>
    /// Versione da installare, <b>già filtrata rispetto a quella installata</b>: null quando non c'è
    /// nulla da installare. Unico punto di verità per banner, badge, pagina Aggiornamenti e riga di
    /// riepilogo: una versione uguale a quella installata NON è un aggiornamento disponibile, anche
    /// se è ciò che l'ultimo controllo aveva memorizzato.
    /// </summary>
    string? AvailableUpdateVersion { get; }

    /// <summary>True mentre un download è in corso.</summary>
    bool IsDownloading { get; }

    /// <summary>Avanzamento del download in corso, 0.0-1.0.</summary>
    double DownloadProgress { get; }

    /// <summary>Esito dell'ultimo download completato in questa sessione; null se nessuno ancora avvenuto.</summary>
    UpdateDownloadResult? LastDownloadResult { get; }

    /// <summary>Sollevato a ogni variazione di stato del download (avanzamento, completamento).</summary>
    event EventHandler? StateChanged;

    /// <summary>Interroga la Release remota e confronta la versione con quella installata.</summary>
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Riconcilia lo stato persistito dell'ultimo controllo con la versione installata, <b>senza
    /// rete</b>: se la versione annunciata come disponibile risulta installata, l'aggiornamento è
    /// stato installato e quello stato viene azzerato (incluso l'eventuale silenziamento di quella
    /// stessa versione). Da chiamare a ogni passaggio in foreground, prima di
    /// <see cref="CheckForUpdateIfDueAsync"/>: senza, l'esito persistito sopravvive
    /// all'aggiornamento e continua ad annunciare la versione che si sta già usando.
    /// Idempotente; NON altera la data/ora dell'ultimo controllo.
    /// </summary>
    void ReconcileInstalledVersion();

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
