using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CardMaster.Services.Ai;

public sealed class OpenAiApiClient : IOpenAiClient
{
    private static readonly TimeSpan VerifyTimeout = TimeSpan.FromSeconds(20);
    private readonly HttpClient _httpClient;
    public OpenAiApiClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<AiKeyCheckResult> VerifyKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return AiKeyCheckResult.Failed(AiErrorKind.NoKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(VerifyTimeout);
        using var request = CreateRequest(HttpMethod.Get, "https://api.openai.com/v1/models", apiKey);
        try
        {
            using var response = await _httpClient.SendAsync(request, timeout.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode ? AiKeyCheckResult.Ok() : AiKeyCheckResult.Failed(await MapResponseAsync(response).ConfigureAwait(false));
        }
        catch (Exception ex) { return AiKeyCheckResult.Failed(MapException(ex, cancellationToken, timeout.Token)); }
    }

    public async Task<OpenAiChatResult> CompleteChatAsync(string apiKey, string model, string requestBody, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "https://api.openai.com/v1/chat/completions", apiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return OpenAiChatResult.Failed(await MapResponseAsync(response).ConfigureAwait(false));
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            var root = document.RootElement;
            var choice = root.GetProperty("choices")[0];
            if (choice.GetProperty("finish_reason").GetString() == "length") return OpenAiChatResult.Failed(AiErrorKind.MalformedResponse);
            var content = choice.GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content)) return OpenAiChatResult.Failed(AiErrorKind.MalformedResponse);
            var usage = root.GetProperty("usage");
            return OpenAiChatResult.Ok(content, usage.GetProperty("prompt_tokens").GetInt64(), usage.GetProperty("completion_tokens").GetInt64());
        }
        catch (Exception ex) { return OpenAiChatResult.Failed(MapException(ex, cancellationToken, cancellationToken)); }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, string key)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Trim());
        return request;
    }

    private static async Task<AiErrorKind> MapResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return AiErrorKind.KeyRejected;
        if (response.StatusCode == (HttpStatusCode)429) return AiErrorKind.RateLimited;
        if ((int)response.StatusCode >= 500) return AiErrorKind.Service;
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return body.Contains("credit", StringComparison.OrdinalIgnoreCase) || body.Contains("billing", StringComparison.OrdinalIgnoreCase) ? AiErrorKind.CreditExhausted : AiErrorKind.Service;
    }

    private static AiErrorKind MapException(Exception ex, CancellationToken userToken, CancellationToken timeoutToken) => ex switch
    {
        OperationCanceledException when userToken.IsCancellationRequested => throw ex,
        OperationCanceledException when timeoutToken.IsCancellationRequested => AiErrorKind.Timeout,
        HttpRequestException => AiErrorKind.Network,
        _ => AiErrorKind.Service,
    };
}
