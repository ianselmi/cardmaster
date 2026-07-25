using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class SharePage : ContentPage
{
    private readonly ShareCardViewModel _viewModel;

    public SharePage(ShareCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
        if (!_viewModel.CardExists)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
