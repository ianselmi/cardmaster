using SkiaSharp;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Prepara la foto dello scontrino per l'invio.
/// <para>
/// Il ridimensionamento non è solo un'ottimizzazione di costo: è l'unico punto in cui si può
/// <b>ridurre il dato che esce dal device</b> senza perdere la funzione. Una foto a piena
/// risoluzione può costare fino a tre volte i token di una ridimensionata, e uno scontrino
/// resta perfettamente leggibile molto prima di quel limite.
/// </para>
/// </summary>
internal static class ReceiptAiImage
{
    /// <summary>
    /// Lato lungo massimo, in pixel. Punto di partenza dichiarato in <c>design.md</c>: va
    /// verificato sugli scontrini reali, non deciso a tavolino.
    /// </summary>
    public const int MaxLongEdge = 1568;

    /// <summary>Qualità JPEG: sopra questa soglia crescono i byte, non la leggibilità del testo.</summary>
    private const int JpegQuality = 85;

    public const string MediaType = "image/jpeg";

    /// <summary>
    /// Riduce l'immagine al lato lungo massimo e la ricodifica in JPEG. Restituisce <c>null</c>
    /// se i byte non sono un'immagine decodificabile — un fallimento qui non deve diventare
    /// un'eccezione a metà del percorso di rilettura.
    /// </summary>
    public static byte[]? Downscale(byte[] imageBytes)
    {
        if (imageBytes is not { Length: > 0 })
        {
            return null;
        }

        using var original = SKBitmap.Decode(imageBytes);
        if (original is null || original.Width <= 0 || original.Height <= 0)
        {
            return null;
        }

        var longEdge = Math.Max(original.Width, original.Height);
        var scale = longEdge <= MaxLongEdge ? 1d : (double)MaxLongEdge / longEdge;

        var width = Math.Max(1, (int)Math.Round(original.Width * scale));
        var height = Math.Max(1, (int)Math.Round(original.Height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        if (surface is null)
        {
            return null;
        }

        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawBitmap(original, new SKRect(0, 0, width, height));
        surface.Canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return data?.ToArray();
    }
}
