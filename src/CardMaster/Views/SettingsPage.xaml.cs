using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Al ritorno dalla pagina Backup lo stato può essere cambiato (abilitato, disabilitato,
        // riconnesso, o un backup appena riuscito che ha spento il segnale di problema).
        _viewModel.RefreshBackupState();
    }

    private async void OnBackupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("BackupPage");
    }

    private async void OnUpdateClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("UpdatePage");
    }

    private async void OnClearReceiptImagesClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Eliminare le immagini?",
            "Gli scontrini restano, con i dati e il testo riconosciuto: si perde solo la foto originale.",
            "Elimina",
            "Annulla");

        if (!confirmed)
        {
            return;
        }

        await _viewModel.ClearReceiptImagesAsync();
    }
}
