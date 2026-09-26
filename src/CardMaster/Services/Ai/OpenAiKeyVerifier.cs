namespace CardMaster.Services.Ai;

public sealed class OpenAiKeyVerifier : IAiKeyVerifier
{
    private readonly IOpenAiClient _client;
    public OpenAiKeyVerifier(IOpenAiClient client) => _client = client;
    public Task<AiKeyCheckResult> VerifyAsync(string apiKey, CancellationToken cancellationToken = default) => _client.VerifyKeyAsync(apiKey, cancellationToken);
}
