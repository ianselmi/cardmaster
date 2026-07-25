using CardMaster.Data;

namespace CardMaster.Services;

/// <summary>Esito della decodifica di un testo scansionato.</summary>
public enum CardShareDecodeStatus
{
    /// <summary>Payload CardMaster valido: <see cref="CardShareDecodeResult.Snapshot"/> è valorizzato.</summary>
    Recognized,

    /// <summary>Il testo non è un payload CardMaster (va trattato come barcode normale).</summary>
    NotCardMaster,

    /// <summary>Prefisso CardMaster presente ma versione dello schema non supportata.</summary>
    Unsupported,

    /// <summary>Prefisso CardMaster presente ma payload illeggibile/corrotto o incompleto.</summary>
    Corrupt,
}

/// <summary>Risultato tipizzato di <see cref="ICardShareCodec.TryDecode"/>.</summary>
/// <param name="Status">Stato della decodifica.</param>
/// <param name="Snapshot">Snapshot ricostruito (solo quando <see cref="Status"/> è Recognized).</param>
public sealed record CardShareDecodeResult(CardShareDecodeStatus Status, CardShareSnapshot? Snapshot)
{
    public static readonly CardShareDecodeResult NotCardMaster = new(CardShareDecodeStatus.NotCardMaster, null);
    public static readonly CardShareDecodeResult Unsupported = new(CardShareDecodeStatus.Unsupported, null);
    public static readonly CardShareDecodeResult Corrupt = new(CardShareDecodeStatus.Corrupt, null);

    public static CardShareDecodeResult Ok(CardShareSnapshot snapshot) =>
        new(CardShareDecodeStatus.Recognized, snapshot);
}

/// <summary>
/// Serializza e deserializza lo snapshot di una carta per il QR di condivisione
/// (payload versionato con magic prefix). <see cref="TryDecode"/> NON lancia mai:
/// restituisce sempre un <see cref="CardShareDecodeResult"/> con lo stato adeguato.
/// </summary>
public interface ICardShareCodec
{
    /// <summary>Produce il testo da codificare nel QR a partire dallo snapshot.</summary>
    string Encode(CardShareSnapshot snapshot);

    /// <summary>Tenta di decodificare un testo scansionato in uno snapshot.</summary>
    CardShareDecodeResult TryDecode(string? text);
}
