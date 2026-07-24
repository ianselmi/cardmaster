using CardMaster.Data;
using SkiaSharp;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace CardMaster.Services;

/// <summary>
/// Rendering barcode con ZXing.Net + SkiaSharp. Produce PNG nero-su-bianco.
/// Robusto: qualunque errore (formato non mappabile, valore non conforme) → esito fallito.
/// </summary>
public sealed class BarcodeRenderer : IBarcodeRenderer
{
    public BarcodeRenderResult Render(string value, string format)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BarcodeRenderResult.Fail();
        }

        var zxingFormat = BarcodeFormatCatalog.ToZXing(format);
        if (zxingFormat is null)
        {
            return BarcodeRenderResult.Fail();
        }

        try
        {
            var (width, height) = BarcodeFormatCatalog.Is2D(format) ? (500, 500) : (600, 260);

            var writer = new BarcodeWriter
            {
                Format = zxingFormat.Value,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10,
                    PureBarcode = true,
                },
            };

            using var bitmap = writer.Write(value);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var bytes = data.ToArray();
            return BarcodeRenderResult.Ok(ImageSource.FromStream(() => new MemoryStream(bytes)));
        }
        catch
        {
            // Valore non conforme al formato o altro problema di generazione: nessun crash.
            return BarcodeRenderResult.Fail();
        }
    }
}
