using BarcodeScanning;
using CardMaster.Data;

namespace CardMaster.Views;

public partial class ScanPage : ContentPage
{
    private bool _handled;

    public ScanPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _handled = false;

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

            // Ferma alla prima lettura valida.
            _handled = true;
            Camera.CameraEnabled = false;

            await MainThread.InvokeOnMainThreadAsync(() =>
                Shell.Current.GoToAsync("AddCardPage", new Dictionary<string, object>
                {
                    ["barcode"] = value,
                    ["format"] = format,
                }));
            return;
        }
    }

    private async void OnManualEntryClicked(object? sender, EventArgs e)
    {
        Camera.CameraEnabled = false;
        await Shell.Current.GoToAsync("AddCardPage");
    }
}
