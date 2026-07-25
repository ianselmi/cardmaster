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
        => await Shell.Current.GoToAsync("SettingsPage");

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
