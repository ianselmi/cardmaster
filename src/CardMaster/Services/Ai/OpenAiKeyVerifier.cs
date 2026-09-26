namespace CardMaster.Services.Ai;

public sealed class OpenAiKeyVerifier : IAiKeyVerifier
{
    private readonly IAClientFactory _clients;
    public OpenAiKeyVerifier(IAClientFactory clients) => _clients = clients;
    public Task<AiKeyCheckResult> VerifyAsync(string apiKey, CancellationToken cancellationToken = default) =>
        _clients.Create(new AClientConfiguration(apiKey)).VerifyKeyAsync(cancellationToken);
}
