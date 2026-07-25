using CardMaster.Services;
using CardMaster.Services.Backup;
using CardMaster.ViewModels;

namespace CardMaster;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell, ISettingsStore settings, IBackupService backup)
    {
        InitializeComponent();
        _shell = shell;

        // Applica il tema persistito all'avvio (System→Unspecified segue il dispositivo).
        SettingsViewModel.ApplyTheme(settings.Theme);

        // Backup "a ogni apertura": no-op se disabilitato, altra frequenza o rete assente.
        _ = backup.MaybeBackupOnOpenAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_shell);
    }
}
