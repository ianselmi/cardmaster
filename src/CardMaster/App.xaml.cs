using CardMaster.Services;
using CardMaster.ViewModels;

namespace CardMaster;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell, ISettingsStore settings)
    {
        InitializeComponent();
        _shell = shell;

        // Applica il tema persistito all'avvio (System→Unspecified segue il dispositivo).
        SettingsViewModel.ApplyTheme(settings.Theme);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_shell);
    }
}
