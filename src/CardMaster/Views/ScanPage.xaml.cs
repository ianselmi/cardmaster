using BarcodeScanning;
using CardMaster.Data;
using CardMaster.Services;

namespace CardMaster.Views;

public partial class ScanPage : ContentPage
{
    private readonly ICardShareCodec _shareCodec;
    private bool _handled;

    // Ultimo valore scartato (payload CardMaster non leggibile): evita di ripetere
    // l'avviso a raffica mentre lo stesso QR resta inquadrato.
    private string? _lastRejectedValue;

    // Serve a riabilitare la camera dopo un'analisi da immagine senza esito,
    // ma solo se il permesso c'è davvero.
    private bool _cameraPermissionGranted;

    public ScanPage(ICardShareCodec shareCodec)
    {
        InitializeComponent();
        _shareCodec = shareCodec;
    }

    /// <summary>
    /// Esito dell'interpretazione di un codice letto — dalla camera o da un'immagine:
    /// snapshot di condivisione CardMaster, barcode fedeltà grezzo, oppure payload
    /// CardMaster non leggibile (<paramref name="Rejection"/> valorizzato).
    /// </summary>
    private sealed record CodeOutcome(
        CardShareSnapshot? Snapshot,
        string? Barcode,
        string? Format,
        CardShareDecodeStatus? Rejection);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _handled = false;
        _lastRejectedValue = null;

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        _cameraPermissionGranted = status == PermissionStatus.Granted;

        PermissionDeniedPanel.IsVisible = !_cameraPermissionGranted;
        Camera.BarcodeSymbologies = BarcodeFormatCatalog.ScannerSymbologies;
        Camera.CameraEnabled = _cameraPermissionGranted;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Camera.CameraEnabled = false;
    }

    private async void OnDetectionFinished(object? sender, OnDetectionFinishedEventArg e)
    {
        if (_handled || e.BarcodeResults is null || e.BarcodeResults.Count == 0)
        {
            return;
        }

        foreach (var result in e.BarcodeResults)
        {
            var format = BarcodeFormatCatalog.FromScanner(result.BarcodeFormat);
            if (format is null)
            {
                continue;
            }

            var value = string.IsNullOrEmpty(result.DisplayValue) ? result.RawValue : result.DisplayValue;
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            var outcome = Interpret(value, format);
            if (outcome.Rejection is { } rejection)
            {
                await RejectAsync(value, rejection);
                return;
            }

            _handled = true;
            Camera.CameraEnabled = false;
            await NavigateToConfirmationAsync(outcome);
            return;
        }
    }

    /// <summary>
    /// Decide come trattare un codice già validato (formato supportato, valore non vuoto),
    /// indipendentemente da dove è stato letto.
    /// </summary>
    private CodeOutcome Interpret(string value, string format)
    {
        // Per i QR, verifica se è un payload di condivisione CardMaster.
        if (format == BarcodeFormatCatalog.QrCode)
        {
            var decoded = _shareCodec.TryDecode(value);
            switch (decoded.Status)
            {
                case CardShareDecodeStatus.Recognized:
                    return new CodeOutcome(decoded.Snapshot, null, null, null);

                case CardShareDecodeStatus.Unsupported:
                case CardShareDecodeStatus.Corrupt:
                    return new CodeOutcome(null, null, null, decoded.Status);

                // NotCardMaster: prosegue come normale barcode QR.
            }
        }

        return new CodeOutcome(null, value, format, null);
    }

    /// <summary>Apre la conferma pre-compilata per un esito valido (mai una rejection).</summary>
    private Task NavigateToConfirmationAsync(CodeOutcome outcome)
    {
        var parameters = new Dictionary<string, object>();

        if (outcome.Snapshot is { } snapshot)
        {
            parameters["barcode"] = snapshot.Barcode;
            parameters["format"] = snapshot.BarcodeFormat;
            parameters["name"] = snapshot.DisplayName;

            if (!string.IsNullOrWhiteSpace(snapshot.IssuerName))
            {
                parameters["issuer"] = snapshot.IssuerName;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.Color))
            {
                parameters["color"] = snapshot.Color;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.LogoId))
            {
                parameters["logo"] = snapshot.LogoId;
            }
        }
        else
        {
            parameters["barcode"] = outcome.Barcode!;
            parameters["format"] = outcome.Format!;
        }

        return MainThread.InvokeOnMainThreadAsync(() =>
            Shell.Current.GoToAsync("AddCardPage", parameters));
    }

    private async Task RejectAsync(string value, CardShareDecodeStatus status)
    {
        // Non avvisare in continuazione per lo stesso QR ancora inquadrato.
        if (value == _lastRejectedValue)
        {
            return;
        }

        _lastRejectedValue = value;
        _handled = true;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlertAsync("Impossibile importare", RejectionMessage(status), "OK");
            _handled = false;
        });
    }

    private static string RejectionMessage(CardShareDecodeStatus status) =>
        status == CardShareDecodeStatus.Unsupported
            ? "Questa carta è stata condivisa da una versione più recente dell'app. Aggiorna CardMaster per importarla."
            : "Codice di condivisione CardMaster non leggibile.";

    private async void OnManualEntryClicked(object? sender, EventArgs e)
    {
        Camera.CameraEnabled = false;
        await Shell.Current.GoToAsync("AddCardPage");
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        FileResult? file;
        try
        {
            file = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Scegli l'immagine con il codice",
                FileTypes = FilePickerFileType.Images,
            });
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Impossibile aprire il selettore",
                "Non è stato possibile aprire il selettore di immagini.",
                "OK");
            return;
        }

        // Selezione annullata: si resta sulla pagina, senza messaggi.
        if (file is null)
        {
            return;
        }

        // Durante l'analisi: niente letture live in parallelo, niente doppia analisi.
        _handled = true;
        Camera.CameraEnabled = false;
        PickImageButton.IsEnabled = false;

        try
        {
            var outcome = await ScanImageAsync(file);

            if (outcome is null)
            {
                await DisplayAlertAsync(
                    "Nessun codice trovato",
                    "In questa immagine non è stato trovato un codice a barre supportato. Prova con un'altra immagine, con la camera o inserisci il codice a mano.",
                    "OK");
                ResumeScanning();
                return;
            }

            if (outcome.Rejection is { } rejection)
            {
                await DisplayAlertAsync("Impossibile importare", RejectionMessage(rejection), "OK");
                ResumeScanning();
                return;
            }

            await NavigateToConfirmationAsync(outcome);
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Immagine non leggibile",
                "Non è stato possibile leggere questa immagine. Prova con un'altra.",
                "OK");
            ResumeScanning();
        }
        finally
        {
            PickImageButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Analizza l'immagine con lo stesso motore della scansione live e restituisce il primo
    /// codice di formato supportato, oppure null se non ne contiene nessuno. L'immagine viene
    /// solo letta: l'app non ne conserva copie.
    /// </summary>
    private async Task<CodeOutcome?> ScanImageAsync(FileResult file)
    {
        // ScanFromImageAsync non filtra le simbologie: il filtro sui formati supportati
        // è lo stesso applicato ai risultati della camera.
        var results = await Methods.ScanFromImageAsync(file);
        if (results is null)
        {
            return null;
        }

        foreach (var result in results)
        {
            var format = BarcodeFormatCatalog.FromScanner(result.BarcodeFormat);
            if (format is null)
            {
                continue;
            }

            var value = string.IsNullOrEmpty(result.DisplayValue) ? result.RawValue : result.DisplayValue;
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            return Interpret(value, format);
        }

        return null;
    }

    /// <summary>Riporta la pagina in stato operativo dopo un'analisi senza esito.</summary>
    private void ResumeScanning()
    {
        _handled = false;
        _lastRejectedValue = null;
        Camera.CameraEnabled = _cameraPermissionGranted;
    }
}
