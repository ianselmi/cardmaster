using CardMaster.Data;
using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class CardListPage : ContentPage
{
    private readonly CardListViewModel _viewModel;

    public CardListPage(CardListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnAddClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("ScanPage");

    private async void OnSettingsClicked(object? sender, EventArgs e)
        // Con un aggiornamento segnalato (badge), il tocco porta dritto al flusso di aggiornamento
        // invece che alla lista Impostazioni.
        => await Shell.Current.GoToAsync(_viewModel.IsUpdateAvailable ? "UpdatePage" : "SettingsPage");

    private async void OnUpdateBannerTapped(object? sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("UpdatePage");

    private void OnUpdateBannerDismissTapped(object? sender, TappedEventArgs e)
        => _viewModel.DismissUpdateBanner();

    private async void OnCardSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Card card)
        {
            return;
        }

        // Deseleziona subito, così il tap successivo sulla stessa carta funziona.
        ((CollectionView)sender!).SelectedItem = null;

        await Shell.Current.GoToAsync($"ShowCardPage?id={Uri.EscapeDataString(card.Id)}");
    }
}
