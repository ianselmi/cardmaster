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
}
