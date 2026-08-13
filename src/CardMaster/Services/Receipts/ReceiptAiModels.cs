using CardMaster.Services.Ai;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Quanto è costata davvero una rilettura. Ricavato dal conteggio che la risposta riporta, non
/// stimato: la stima dichiarata nelle impostazioni serve a decidere prima, questo a sapere dopo.
/// </summary>
/// <param name="InputTokens">Token in ingresso (immagine e istruzioni).</param>
/// <param name="OutputTokens">Token in uscita (il JSON delle righe).</param>
/// <param name="Model">Identificativo del modello che ha risposto.</param>
public readonly record struct ReceiptAiUsage(long InputTokens, long OutputTokens, string Model)
{
    /// <summary>
    /// Costo in millesimi di centesimo, secondo il listino del modello. In interi come il resto
    /// del dominio: un costo di quattro centesimi non merita un <c>double</c>.
    /// </summary>
    public long CostMicroCents(ReceiptAiModelOption option) =>
        (InputTokens * option.InputPricePerMillionMicroCents
         + OutputTokens * option.OutputPricePerMillionMicroCents) / 1_000_000;
}

/// <summary>
/// Lettura completa prodotta dal modello: la stessa testata e le stesse righe che produce il
/// parser locale, così l'esito entra nelle strutture esistenti e passa per la <b>stessa</b>
/// <see cref="ReceiptTotalsCheck"/> — non è verità, è un'altra lettura da confrontare.
/// </summary>
public sealed record ReceiptAiReading(
    ReceiptHeader Header,
    IReadOnlyList<ReceiptItemLine> Items,
    ReceiptVatSummary VatSummary,
    ReceiptAiUsage Usage);

/// <summary>
/// Esito della rilettura: o una lettura, o una causa d'errore riconoscibile. Mai entrambe, mai
/// nessuna delle due — e mai righe parziali ricavate da una risposta incompleta.
/// </summary>
public sealed record ReceiptAiResult(ReceiptAiReading? Reading, AiErrorKind Error)
{
    public bool Succeeded => Reading is not null;

    public static ReceiptAiResult Ok(ReceiptAiReading reading) => new(reading, AiErrorKind.None);

    public static ReceiptAiResult Failed(AiErrorKind error) => new(null, error);
}

/// <summary>
/// Un modello selezionabile, con il suo prezzo di listino. I prezzi stanno qui e non sparsi
/// nell'interfaccia perché il costo indicativo mostrato prima e quello effettivo calcolato dopo
/// devono venire dalla stessa fonte, altrimenti divergono alla prima variazione di listino.
/// </summary>
/// <param name="Id">
/// Identificativo esatto del modello. Preso dal listino ufficiale e mai costruito a mano:
/// aggiungere un suffisso di data a un alias produce un 404.
/// </param>
/// <param name="DisplayName">Nome mostrato nelle impostazioni.</param>
/// <param name="InputPricePerMillionMicroCents">Prezzo per milione di token in ingresso, in millesimi di centesimo.</param>
/// <param name="OutputPricePerMillionMicroCents">Prezzo per milione di token in uscita, in millesimi di centesimo.</param>
public sealed record ReceiptAiModelOption(
    string Id,
    string DisplayName,
    long InputPricePerMillionMicroCents,
    long OutputPricePerMillionMicroCents);

/// <summary>
/// I modelli tra cui l'utente può scegliere, con il costo per scontrino dichiarato accanto.
/// </summary>
public static class ReceiptAiModels
{
    /// <summary>
    /// Default. È il caso in cui la lettura conta più del costo: qui ci si arriva <b>solo dopo</b>
    /// che la quadratura locale è fallita, quindi si sta già pagando un errore.
    /// </summary>
    public const string DefaultModelId = "claude-opus-5";

    /// <summary>
    /// Listino in dollari per milione di token, convertito in millesimi di centesimo:
    /// $5 → 500.000. Interi, nessuna virgola mobile.
    /// </summary>
    public static IReadOnlyList<ReceiptAiModelOption> All { get; } =
    [
        new("claude-opus-5", "Claude Opus 5", 500_000, 2_500_000),
        new("claude-sonnet-5", "Claude Sonnet 5", 300_000, 1_500_000),
        new("claude-haiku-4-5", "Claude Haiku 4.5", 100_000, 500_000),
    ];

    /// <summary>
    /// Il modello con quell'identificativo, o il default se l'identificativo non è più tra quelli
    /// noti — così una preferenza salvata da una versione precedente non blocca la funzione.
    /// </summary>
    public static ReceiptAiModelOption Resolve(string? id) =>
        All.FirstOrDefault(m => m.Id == id) ?? All[0];
}
