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

        await _viewModel.LoadAsync();
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
}
