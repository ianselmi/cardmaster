using CardMaster.Services;
using CardMaster.Services.Ai;

namespace CardMaster.Services.Receipts;

public sealed class OpenAiReceiptAiReader : IReceiptAiReader
{
    private const int MaxOutputTokens = 8000;
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);
    private readonly IAiCredentialStore _credentials;
    private readonly ISettingsStore _settings;
    private readonly IAClientFactory _clients;

    public OpenAiReceiptAiReader(IAiCredentialStore credentials, ISettingsStore settings, IAClientFactory clients)
    {
        _credentials = credentials;
        _settings = settings;
        _clients = clients;
    }

    public async Task<ReceiptAiResult> ReadAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var apiKey = await _credentials.GetKeyAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(apiKey)) return ReceiptAiResult.Failed(AiErrorKind.NoKey);
        var downscaled = ReceiptAiImage.Downscale(imageBytes);
        if (downscaled is null) return ReceiptAiResult.Failed(AiErrorKind.MalformedResponse);
        var option = ReceiptAiModels.Resolve(_settings.AiScanModelId);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        var client = _clients.Create(new AClientConfiguration(apiKey));
        var response = await client.CompleteChatAsync(option.Id, OpenAiRequestBody.Build(option.Id, MaxOutputTokens, downscaled), timeout.Token).ConfigureAwait(false);
        if (!response.Succeeded || response.Content is null) return ReceiptAiResult.Failed(response.Error);
        var usage = new ReceiptAiUsage(response.InputTokens, response.OutputTokens, option.Id);
        var result = ReceiptAiMapper.Map(response.Content, usage);
        if (result.Succeeded)
        {
            _settings.LastAiScanInputTokens = usage.InputTokens;
            _settings.LastAiScanOutputTokens = usage.OutputTokens;
            _settings.LastAiScanCostMicroCents = usage.CostMicroCents(option);
        }
        return result;
    }
}
