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

    private async void OnBackupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("BackupPage");
    }
}
