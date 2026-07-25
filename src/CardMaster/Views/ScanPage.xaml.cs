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

    public ScanPage(ICardShareCodec shareCodec)
    {
        InitializeComponent();
        _shareCodec = shareCodec;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _handled = false;
        _lastRejectedValue = null;

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        var granted = status == PermissionStatus.Granted;

        PermissionDeniedPanel.IsVisible = !granted;
        Camera.BarcodeSymbologies = BarcodeFormatCatalog.ScannerSymbologies;
        Camera.CameraEnabled = granted;
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

            // Per i QR, verifica se è un payload di condivisione CardMaster.
            if (format == BarcodeFormatCatalog.QrCode)
            {
                var decoded = _shareCodec.TryDecode(value);
                switch (decoded.Status)
                {
                    case CardShareDecodeStatus.Recognized:
                        await HandleSharedCardAsync(decoded.Snapshot!);
                        return;

                    case CardShareDecodeStatus.Unsupported:
                    case CardShareDecodeStatus.Corrupt:
                        await RejectAsync(value, decoded.Status);
                        return;

                    // NotCardMaster: prosegue come normale barcode QR.
                }
            }

            await HandleRawBarcodeAsync(value, format);
            return;
        }
    }

    private async Task HandleRawBarcodeAsync(string value, string format)
    {
        _handled = true;
        Camera.CameraEnabled = false;

        await MainThread.InvokeOnMainThreadAsync(() =>
            Shell.Current.GoToAsync("AddCardPage", new Dictionary<string, object>
            {
                ["barcode"] = value,
                ["format"] = format,
            }));
    }

    private async Task HandleSharedCardAsync(CardShareSnapshot snapshot)
    {
        _handled = true;
        Camera.CameraEnabled = false;

        var parameters = new Dictionary<string, object>
        {
            ["barcode"] = snapshot.Barcode,
            ["format"] = snapshot.BarcodeFormat,
            ["name"] = snapshot.DisplayName,
        };

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

        await MainThread.InvokeOnMainThreadAsync(() =>
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

        var message = status == CardShareDecodeStatus.Unsupported
            ? "Questa carta è stata condivisa da una versione più recente dell'app. Aggiorna CardMaster per importarla."
            : "Codice di condivisione CardMaster non leggibile.";

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlertAsync("Impossibile importare", message, "OK");
            _handled = false;
        });
    }

    private async void OnManualEntryClicked(object? sender, EventArgs e)
    {
        Camera.CameraEnabled = false;
        await Shell.Current.GoToAsync("AddCardPage");
    }
}
