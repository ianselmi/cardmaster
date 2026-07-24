using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class AddCardPage : ContentPage
{
    private readonly AddCardViewModel _viewModel;

    public AddCardPage(AddCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.Validate(out var error))
        {
            await DisplayAlertAsync("Dati mancanti", error, "OK");
            return;
        }

        if (await _viewModel.BarcodeExistsAsync())
        {
            var addAnyway = await DisplayAlertAsync(
                "Hai già questa carta",
                "Esiste già una carta con questo codice. Vuoi aggiungerla comunque?",
                "Aggiungi comunque",
                "Annulla");

            if (!addAnyway)
            {
                return;
            }
        }

        await _viewModel.SaveAsync();

        // Torna alla lista (radice dello Shell), che si ricarica in OnAppearing.
        await Shell.Current.GoToAsync("//CardListPage");
    }
}
