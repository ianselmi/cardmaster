namespace CardMaster.Services.Ai;

/// <summary>
/// Custodia della chiave API <b>dell'utente</b> per la lettura assistita degli scontrini.
/// La chiave sta nell'archivio protetto del sistema operativo (Keystore Android via
/// <see cref="Microsoft.Maui.Storage.SecureStorage"/>): mai nelle preferenze in chiaro, mai nel
/// database, mai nel backup su Drive, mai nei log.
/// </summary>
/// <remarks>
/// Il repository è pubblico e l'APK scaricabile da chiunque: una chiave nostra nel pacchetto
/// sarebbe estraibile in cinque minuti e la pagherebbe l'autore per tutti. L'unica architettura
/// compatibile senza un server è la chiave dell'utente — vedi <c>openspec/specs/ai-credentials</c>.
/// </remarks>
public interface IAiCredentialStore
{
    /// <summary>
    /// Se una chiave risulta configurata. Pensata per l'interfaccia: risponde subito e
    /// <b>non tocca il valore</b>, che resta nell'archivio protetto.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Conserva la chiave, sostituendo quella eventualmente presente. Gli spazi attorno vengono
    /// rimossi (una chiave incollata se li porta dietro spesso). Una stringa vuota è rifiutata.
    /// </summary>
    /// <returns>Falso se la chiave era vuota o l'archivio protetto non era disponibile.</returns>
    Task<bool> SetKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rimuove la chiave. Le funzioni che la richiedono tornano indisponibili;
    /// <b>gli scontrini già salvati non vengono toccati</b>.
    /// </summary>
    Task RemoveKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Legge la chiave per effettuare una chiamata. <b>Solo per il client del modello</b>: il
    /// valore non va mostrato nell'interfaccia, registrato nei log, né incluso nei messaggi
    /// d'errore. Null se nessuna chiave è configurata.
    /// </summary>
    Task<string?> GetKeyAsync(CancellationToken cancellationToken = default);
}
