namespace CardMaster.Services.Ai;

public sealed record AClientConfiguration(string ApiKey);

public interface IAClient
{
    Task<OpenAiChatResult> CompleteChatAsync(string model, string requestBody, CancellationToken cancellationToken = default);
    Task<AiKeyCheckResult> VerifyKeyAsync(CancellationToken cancellationToken = default);
}

public interface IAClientFactory
{
    IAClient Create(AClientConfiguration configuration);
}

public sealed record OpenAiChatResult(bool Succeeded, string? Content, long InputTokens, long OutputTokens, AiErrorKind Error)
{
    public static OpenAiChatResult Ok(string content, long inputTokens, long outputTokens) => new(true, content, inputTokens, outputTokens, AiErrorKind.None);
    public static OpenAiChatResult Failed(AiErrorKind error) => new(false, null, 0, 0, error);
}
