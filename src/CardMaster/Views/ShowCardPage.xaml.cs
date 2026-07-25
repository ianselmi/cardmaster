using CardMaster.Services;
using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class ShowCardPage : ContentPage
{
    private readonly ShowCardViewModel _viewModel;
    private readonly IScreenBrightnessController _brightness;
    private readonly IReadingFilterProbe _readingFilter;

    public ShowCardPage(
        ShowCardViewModel viewModel,
        IScreenBrightnessController brightness,
        IReadingFilterProbe readingFilter)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _brightness = brightness;
        _readingFilter = readingFilter;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ricarica sempre: riflette le eventuali modifiche fatte in EditCardPage.
        await _viewModel.ReloadAsync();
        if (!_viewModel.CardExists)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Ottimizza per la lettura alla cassa.
        DeviceDisplay.Current.KeepScreenOn = true;
        _brightness.SetMax();

        ReadingFilterBanner.IsVisible = _readingFilter.Probe() == ReadingFilterState.Active;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        DeviceDisplay.Current.KeepScreenOn = false;
        _brightness.RestoreDefault();
    }

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.CardId))
        {
            return;
        }

        await Shell.Current.GoToAsync($"SharePage?id={Uri.EscapeDataString(_viewModel.CardId)}");
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.CardId))
        {
            return;
        }

        await Shell.Current.GoToAsync($"EditCardPage?id={Uri.EscapeDataString(_viewModel.CardId)}");
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Elimina carta",
            "Vuoi eliminare questa carta? L'operazione non è annullabile.",
            "Elimina",
            "Annulla");

        if (!confirm)
        {
            return;
        }

        await _viewModel.DeleteAsync();

        // Torna alla lista, che si ricarica in OnAppearing (la carta non comparirà più).
        await Shell.Current.GoToAsync("//CardListPage");
    }
}
