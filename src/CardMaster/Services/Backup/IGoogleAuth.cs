namespace CardMaster.Services.Backup;

/// <summary>
/// Autenticazione Google (OAuth 2.0 Authorization Code + PKCE) per il backup su Drive.
/// Il <c>refresh_token</c> è persistito in <see cref="Microsoft.Maui.Storage.SecureStorage"/>;
/// l'<c>access_token</c> vive in memoria e viene rinnovato on-demand e su 401.
/// Nessuna delle operazioni lancia eccezioni verso il chiamante per annullamento o assenza di rete.
/// </summary>
public interface IGoogleAuth
{
    /// <summary>Email dell'account collegato, o null se nessun account è collegato.</summary>
    string? AccountEmail { get; }

    /// <summary>True se esiste un account collegato (credenziali presenti).</summary>
    bool IsSignedIn { get; }

    /// <summary>
    /// Avvia il consenso Google e scambia il code con i token. Restituisce true se il
    /// collegamento è riuscito, false se l'utente ha annullato o la rete non è disponibile.
    /// </summary>
    Task<bool> SignInAsync(CancellationToken cancellationToken = default);

    /// <summary>Revoca il token e cancella le credenziali locali. Idempotente.</summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restituisce un access token valido, rinnovandolo se necessario. In caso di fallimento
    /// il motivo è nel <see cref="AccessTokenResult.Failure"/>: distinguere "rete assente" da
    /// "credenziali rifiutate" è ciò che evita di chiedere una riconnessione quando manca
    /// soltanto il campo.
    /// </summary>
    Task<AccessTokenResult> GetValidAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}

/// <summary>Motivo per cui non è stato possibile ottenere un access token valido.</summary>
public enum TokenFailure
{
    /// <summary>Nessun fallimento: il token è valido.</summary>
    None,

    /// <summary>Nessun account collegato (nessun refresh token salvato).</summary>
    NoAccount,

    /// <summary>Refresh token rifiutato da Google (scaduto o revocato): serve riconnettersi.</summary>
    Rejected,

    /// <summary>Errore di trasporto: rete assente o endpoint non raggiungibile. Recuperabile.</summary>
    Network,
}

/// <summary>Access token con l'esito del tentativo di ottenerlo. <c>Token</c> null se <c>Failure</c> non è <see cref="TokenFailure.None"/>.</summary>
public sealed record AccessTokenResult(string? Token, TokenFailure Failure);
