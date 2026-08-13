namespace CardMaster.Services.Ai;

/// <summary>
/// Verifica che una chiave sia accettata dal servizio, con la richiesta più piccola possibile.
/// Separato da <see cref="IAiCredentialStore"/>: quello custodisce, questo parla in rete.
/// </summary>
public interface IAiKeyVerifier
{
    /// <summary>
    /// Interroga il servizio con una richiesta minima e senza costo in token.
    /// Distingue una chiave <b>rifiutata</b> da un problema di <b>rete</b>: nel secondo caso non
    /// sappiamo se la chiave sia buona, e dichiararla non valida sarebbe una bugia.
    /// </summary>
    /// <param name="apiKey">
    /// La chiave da provare. Passata esplicitamente per poter verificare quella appena digitata
    /// prima di conservarla.
    /// </param>
    Task<AiKeyCheckResult> VerifyAsync(string apiKey, CancellationToken cancellationToken = default);
}
