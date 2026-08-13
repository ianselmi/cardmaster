using Anthropic;
using Anthropic.Exceptions;

namespace CardMaster.Services.Ai;

/// <summary>
/// Verifica la chiave elencando i modelli disponibili: è la richiesta autenticata più piccola
/// dell'API e <b>non consuma token</b>, quindi provare una chiave non costa niente all'utente.
/// </summary>
public sealed class AnthropicKeyVerifier : IAiKeyVerifier
{
    /// <summary>
    /// Oltre questo tempo la verifica si dichiara fallita per rete. Breve di proposito: è
    /// un'azione interattiva, l'utente sta guardando le impostazioni e aspetta una risposta.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    public async Task<AiKeyCheckResult> VerifyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var trimmed = apiKey?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return AiKeyCheckResult.Failed(AiErrorKind.NoKey);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            var client = new AnthropicClient { ApiKey = trimmed };
            await client.Models.List(cancellationToken: timeout.Token).ConfigureAwait(false);
            return AiKeyCheckResult.Ok();
        }
        catch (Exception ex)
        {
            var kind = AiErrorMapper.Classify(ex, cancellationToken, timeout.Token);
            if (kind is null)
            {
                throw;
            }

            return AiKeyCheckResult.Failed(kind.Value);
        }
    }
}

/// <summary>
/// Traduce le eccezioni dell'SDK nelle categorie d'errore del dominio. Vive a parte perché la
/// stessa mappatura serve alla verifica della chiave e alla rilettura dello scontrino: due
/// tabelle separate finirebbero per divergere.
/// </summary>
internal static class AiErrorMapper
{
    /// <summary>
    /// Restituisce la categoria corrispondente, oppure <c>null</c> se l'eccezione non è nostra
    /// e va rilanciata — un annullamento chiesto dall'utente non è un errore da mostrare.
    /// </summary>
    public static AiErrorKind? Classify(Exception ex, CancellationToken userToken, CancellationToken timeoutToken) => ex switch
    {
        // L'annullamento dell'utente si propaga; solo lo scadere del nostro timeout è un errore.
        OperationCanceledException when userToken.IsCancellationRequested => null,
        OperationCanceledException when timeoutToken.IsCancellationRequested => AiErrorKind.Timeout,
        OperationCanceledException => AiErrorKind.Timeout,

        AnthropicUnauthorizedException => AiErrorKind.KeyRejected,
        AnthropicForbiddenException => AiErrorKind.KeyRejected,
        AnthropicRateLimitException => AiErrorKind.RateLimited,

        // Il credito esaurito arriva come 400: si riconosce dal messaggio, non da un tipo
        // dedicato. Euristica dichiarata — se non la riconosciamo resta un errore di servizio,
        // mai un "chiave non valida", che manderebbe l'utente a correggere la cosa sbagliata.
        AnthropicBadRequestException bad when MentionsCredit(bad.Message) => AiErrorKind.CreditExhausted,

        AnthropicIOException => AiErrorKind.Network,
        HttpRequestException => AiErrorKind.Network,
        Anthropic5xxException => AiErrorKind.Service,
        AnthropicApiException => AiErrorKind.Service,
        _ => null,
    };

    private static bool MentionsCredit(string? message) =>
        message is not null
        && (message.Contains("credit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("billing", StringComparison.OrdinalIgnoreCase));
}
