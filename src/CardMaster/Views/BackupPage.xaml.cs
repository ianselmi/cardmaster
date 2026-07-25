using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class BackupPage : ContentPage
{
    private readonly BackupViewModel _viewModel;

    public BackupPage(BackupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }
}
