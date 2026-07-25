using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CardMaster.Data;

namespace CardMaster.Services;

/// <summary>
/// Codec del payload di condivisione: <c>CMC</c> + cifra di versione + JSON compatto
/// con chiavi corte (n/i/b/f/c/l). I campi opzionali vuoti vengono omessi per contenere
/// la dimensione del QR. Serializzazione con Utf8JsonWriter/JsonDocument (niente reflection:
/// robusto al trimming e all'AOT). Nessuna eccezione esce da <see cref="TryDecode"/>.
/// </summary>
public sealed class CardShareCodec : ICardShareCodec
{
    private const string Magic = "CMC";
    private const int SchemaVersion = 1;

    // Escaping rilassato: non c'è contesto HTML (il payload finisce in un QR), quindi
    // accenti e caratteri come & vengono lasciati come UTF-8 anziché \uXXXX, riducendo
    // la dimensione del QR sui nomi accentati (comuni tra gli emittenti italiani).
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Encode(CardShareSnapshot snapshot)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("n", snapshot.DisplayName ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(snapshot.IssuerName))
            {
                writer.WriteString("i", snapshot.IssuerName);
            }

            writer.WriteString("b", snapshot.Barcode ?? string.Empty);
            writer.WriteString("f", snapshot.BarcodeFormat ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(snapshot.Color))
            {
                writer.WriteString("c", snapshot.Color);
            }

            if (!string.IsNullOrWhiteSpace(snapshot.LogoId))
            {
                writer.WriteString("l", snapshot.LogoId);
            }

            writer.WriteEndObject();
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());
        return $"{Magic}{SchemaVersion}{json}";
    }

    public CardShareDecodeResult TryDecode(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.StartsWith(Magic, StringComparison.Ordinal))
        {
            return CardShareDecodeResult.NotCardMaster;
        }

        // Struttura attesa: "CMC" + <versione intera> + JSON (che inizia con '{').
        var braceIndex = text.IndexOf('{');
        if (braceIndex <= Magic.Length)
        {
            return CardShareDecodeResult.Corrupt;
        }

        var versionText = text[Magic.Length..braceIndex];
        if (!int.TryParse(versionText, out var version))
        {
            return CardShareDecodeResult.Corrupt;
        }

        if (version != SchemaVersion)
        {
            return CardShareDecodeResult.Unsupported;
        }

        try
        {
            using var doc = JsonDocument.Parse(text[braceIndex..]);
            var root = doc.RootElement;

            var barcode = ReadString(root, "b");
            var format = ReadString(root, "f");
            if (string.IsNullOrWhiteSpace(barcode) || string.IsNullOrWhiteSpace(format))
            {
                return CardShareDecodeResult.Corrupt;
            }

            var snapshot = new CardShareSnapshot(
                DisplayName: ReadString(root, "n") ?? string.Empty,
                IssuerName: ReadString(root, "i"),
                Barcode: barcode,
                BarcodeFormat: format,
                Color: ReadString(root, "c"),
                LogoId: ReadString(root, "l"));

            return CardShareDecodeResult.Ok(snapshot);
        }
        catch (JsonException)
        {
            return CardShareDecodeResult.Corrupt;
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
}
