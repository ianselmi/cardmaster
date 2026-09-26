namespace CardMaster.Services.Ai;

public interface IOpenAiClient
{
    Task<OpenAiChatResult> CompleteChatAsync(string apiKey, string model, string requestBody, CancellationToken cancellationToken = default);
    Task<AiKeyCheckResult> VerifyKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}

public sealed record OpenAiChatResult(bool Succeeded, string? Content, long InputTokens, long OutputTokens, AiErrorKind Error)
{
    public static OpenAiChatResult Ok(string content, long inputTokens, long outputTokens) => new(true, content, inputTokens, outputTokens, AiErrorKind.None);
    public static OpenAiChatResult Failed(AiErrorKind error) => new(false, null, 0, 0, error);
}
