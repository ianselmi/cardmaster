using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class EditCardPage : ContentPage
{
    private readonly EditCardViewModel _viewModel;

    public EditCardPage(EditCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.InitializeAsync();
        await _viewModel.LoadAsync();

        if (!_viewModel.CardExists)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.Validate(out var error))
        {
            await DisplayAlertAsync("Dati mancanti", error, "OK");
            return;
        }

        await _viewModel.SaveAsync();

        // Torna alla carta (ShowCardPage), che ricarica i dati aggiornati in OnAppearing.
        await Shell.Current.GoToAsync("..");
    }
}
