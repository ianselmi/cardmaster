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
    /// Restituisce un access token valido, rinnovandolo se necessario. Null se non c'è un
    /// account collegato, le credenziali sono state revocate o la rete non è disponibile.
    /// </summary>
    Task<string?> GetValidAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}
