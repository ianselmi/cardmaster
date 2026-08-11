using Android.Gms.Extensions;
using CardMaster.Services.Receipts;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;
using MlKitText = Xamarin.Google.MLKit.Vision.Text.Text;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Riconoscimento testo con ML Kit Text Recognition, modello <b>incluso nell'APK</b>
/// (<c>com.google.mlkit:text-recognition</c>, non la variante Play Services che lo scarica
/// al primo uso): funziona al primo avvio senza rete e senza dipendere dai Play Services
/// del device — vincolo dell'offline-first e della distribuzione fuori dal Play Store.
/// </summary>
public sealed class MlKitReceiptOcr : IReceiptOcr, IDisposable
{
    private readonly ITextRecognizer _recognizer =
        TextRecognition.GetClient(TextRecognizerOptions.DefaultOptions);

    public async Task<OcrResult> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return OcrResult.Empty;
        }

        var context = global::Android.App.Application.Context;
        var uri = global::Android.Net.Uri.FromFile(new Java.IO.File(imagePath));
        if (uri is null)
        {
            return OcrResult.Empty;
        }

        using var image = InputImage.FromFilePath(context, uri);
        cancellationToken.ThrowIfCancellationRequested();

        // Process restituisce una Task dei Play Services (Android.Gms.Tasks), non una Task .NET:
        // AsAsync di Android.Gms.Extensions la adatta ad await.
        var recognized = await _recognizer.Process(image).AsAsync<Java.Lang.Object>().ConfigureAwait(false);
        if (recognized is not MlKitText text || string.IsNullOrWhiteSpace(text.GetText()))
        {
            return OcrResult.Empty;
        }

        var blocks = new List<OcrBlock>();
        foreach (var block in text.TextBlocks)
        {
            var lines = new List<OcrLine>();
            foreach (var line in block.Lines)
            {
                lines.Add(new OcrLine(line.Text ?? string.Empty, ToRect(line.BoundingBox)));
            }

            blocks.Add(new OcrBlock(block.Text ?? string.Empty, ToRect(block.BoundingBox), lines));
        }

        return new OcrResult(text.GetText() ?? string.Empty, blocks);
    }

    /// <summary>
    /// Converte il rettangolo Android in quello di MAUI, così che il modello di ritorno
    /// resti privo di tipi di piattaforma e l'allineamento delle colonne (change
    /// <c>receipt-items</c>) sia testabile senza emulatore.
    /// </summary>
    private static Rect ToRect(global::Android.Graphics.Rect? box) =>
        box is null
            ? Rect.Zero
            : new Rect(box.Left, box.Top, box.Width(), box.Height());

    public void Dispose() => _recognizer.Dispose();
}
