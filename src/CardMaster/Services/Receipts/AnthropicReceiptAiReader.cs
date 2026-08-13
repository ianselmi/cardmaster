using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using CardMaster.Services;
using CardMaster.Services.Ai;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Implementazione di <see cref="IReceiptAiReader"/> sull'SDK ufficiale Anthropic, con la chiave
/// dell'utente presa da <see cref="IAiCredentialStore"/> a ogni chiamata.
/// </summary>
/// <remarks>
/// La chiave si rilegge ogni volta invece di tenerla in un client di lunga vita: se l'utente la
/// rimuove dalle impostazioni, la chiamata successiva deve trovare che non c'è più.
/// </remarks>
public sealed class AnthropicReceiptAiReader : IReceiptAiReader
{
    /// <summary>
    /// Tetto sui token in uscita. Uno scontrino della spesa sta ampiamente dentro; il limite
    /// esiste perché una risposta che non finisce mai costerebbe all'utente senza produrre nulla.
    /// </summary>
    private const int MaxOutputTokens = 8000;

    /// <summary>
    /// Oltre questo tempo la rilettura si dichiara fallita. L'utente sta guardando la schermata
    /// di conferma con le righe locali già davanti: può sempre correggerle a mano.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    private readonly IAiCredentialStore _credentials;
    private readonly ISettingsStore _settings;

    public AnthropicReceiptAiReader(IAiCredentialStore credentials, ISettingsStore settings)
    {
        _credentials = credentials;
        _settings = settings;
    }

    public async Task<ReceiptAiResult> ReadAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var apiKey = await _credentials.GetKeyAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(apiKey))
        {
            return ReceiptAiResult.Failed(AiErrorKind.NoKey);
        }

        var downscaled = ReceiptAiImage.Downscale(imageBytes);
        if (downscaled is null)
        {
            // Immagine illeggibile in locale: non ha senso spendere una chiamata per scoprirlo.
            return ReceiptAiResult.Failed(AiErrorKind.MalformedResponse);
        }

        var option = ReceiptAiModels.Resolve(_settings.AiScanModelId);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        Message response;
        try
        {
            var client = new AnthropicClient { ApiKey = apiKey };
            response = await client.Messages.Create(BuildRequest(option, downscaled), timeout.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var kind = AiErrorMapper.Classify(ex, cancellationToken, timeout.Token);
            if (kind is null)
            {
                throw;
            }

            return ReceiptAiResult.Failed(kind.Value);
        }

        // Un rifiuto per policy non è una risposta malformata, ma non produce righe: si dichiara
        // come errore di servizio invece di far credere che lo scontrino fosse illeggibile.
        if (response.StopReason == "refusal")
        {
            return ReceiptAiResult.Failed(AiErrorKind.Service);
        }

        var usage = new ReceiptAiUsage(
            response.Usage?.InputTokens ?? 0,
            response.Usage?.OutputTokens ?? 0,
            option.Id);

        var result = ReceiptAiMapper.Map(ExtractText(response), usage);
        if (result.Succeeded)
        {
            RecordUsage(usage, option);
        }

        return result;
    }

    /// <summary>
    /// La richiesta: l'immagine dello scontrino, le istruzioni, e lo <b>schema</b> della risposta.
    /// Nient'altro dell'utente — nessuno storico, nessuna altra immagine, nessun dato del database.
    /// </summary>
    private static MessageCreateParams BuildRequest(ReceiptAiModelOption option, byte[] image) =>
        new()
        {
            Model = option.Id,
            MaxTokens = MaxOutputTokens,
            System = ReceiptAiPrompt.System,
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = ReceiptAiSchema.ToDictionary() },
            },
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = new List<ContentBlockParam>
                    {
                        new ImageBlockParam
                        {
                            Source = new Base64ImageSource
                            {
                                Data = Convert.ToBase64String(image),
                                MediaType = ReceiptAiImage.MediaType,
                            },
                        },
                        new TextBlockParam { Text = ReceiptAiPrompt.User },
                    },
                },
            ],
        };

    /// <summary>
    /// Il JSON prodotto dal modello. Con <c>output_config.format</c> imposto arriva nel primo
    /// blocco di testo; si concatenano comunque tutti, perché una risposta divisa in più blocchi
    /// non è un errore da trasformare in una lettura troncata.
    /// </summary>
    private static string ExtractText(Message response) =>
        string.Concat(response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .Select(t => t.Text));

    /// <summary>
    /// Registra il consumo <b>effettivo</b>, per poter mostrare quanto è costata davvero l'ultima
    /// chiamata invece della sola stima.
    /// </summary>
    private void RecordUsage(ReceiptAiUsage usage, ReceiptAiModelOption option)
    {
        _settings.LastAiScanInputTokens = usage.InputTokens;
        _settings.LastAiScanOutputTokens = usage.OutputTokens;
        _settings.LastAiScanCostMicroCents = usage.CostMicroCents(option);
    }
}
