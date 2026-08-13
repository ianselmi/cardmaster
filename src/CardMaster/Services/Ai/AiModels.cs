namespace CardMaster.Services.Ai;

/// <summary>
/// Causa riconoscibile di un fallimento nel percorso con il modello. Ogni valore corrisponde a
/// una situazione diversa per l'utente — e quindi a un messaggio diverso su cosa fare — sul
/// modello di <c>BackupErrorKind</c>: una chiave rifiutata si corregge nelle impostazioni, una
/// rete assente si riprova più tardi, e le due non vanno confuse.
/// </summary>
public enum AiErrorKind
{
    /// <summary>Nessun errore.</summary>
    None,

    /// <summary>Nessuna chiave configurata: la funzione non può partire.</summary>
    NoKey,

    /// <summary>Il servizio ha rifiutato la chiave (401/403): va verificata nelle impostazioni.</summary>
    KeyRejected,

    /// <summary>Credito esaurito sull'account: non è un errore dell'app né della chiave.</summary>
    CreditExhausted,

    /// <summary>Troppe richieste in poco tempo (429): riprovare tra qualche minuto.</summary>
    RateLimited,

    /// <summary>Rete assente o non raggiungibile.</summary>
    Network,

    /// <summary>La richiesta non si è conclusa entro il tempo massimo.</summary>
    Timeout,

    /// <summary>Errore del servizio (5xx) o risposta inattesa.</summary>
    Service,

    /// <summary>La risposta non rispetta lo schema: nessuna riga se ne ricava.</summary>
    MalformedResponse,
}

/// <summary>
/// Esito della verifica di una chiave. <see cref="Valid"/> è vero solo quando il servizio ha
/// accettato la chiave: un problema di rete lascia <see cref="Valid"/> falso ma
/// <see cref="Error"/> a <see cref="AiErrorKind.Network"/>, perché non sappiamo se la chiave
/// sia buona — e dirlo è diverso dal dichiararla non valida.
/// </summary>
public sealed record AiKeyCheckResult(bool Valid, AiErrorKind Error)
{
    public static AiKeyCheckResult Ok() => new(true, AiErrorKind.None);

    public static AiKeyCheckResult Failed(AiErrorKind error) => new(false, error);
}
