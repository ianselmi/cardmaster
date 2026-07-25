using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class UpdatePage : ContentPage
{
    private readonly UpdateViewModel _viewModel;

    public UpdatePage(UpdateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Attach();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Detach();
    }
}
