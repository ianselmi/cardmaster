using CardMaster.Services;
using CardMaster.Services.Backup;
using CardMaster.Services.Update;
using CardMaster.ViewModels;

namespace CardMaster;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly IUpdateService _updateService;

    public App(AppShell shell, ISettingsStore settings, IBackupService backup, IUpdateService updateService)
    {
        InitializeComponent();
        _shell = shell;
        _updateService = updateService;

        // Applica il tema persistito all'avvio (System→Unspecified segue il dispositivo).
        SettingsViewModel.ApplyTheme(settings.Theme);

        // Backup "a ogni apertura": no-op se disabilitato, altra frequenza o rete assente.
        _ = backup.MaybeBackupOnOpenAsync();

        // Controllo aggiornamenti automatico: no-op se l'opzione non è attiva o l'intervallo minimo non è trascorso.
        _ = _updateService.CheckForUpdateIfDueAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_shell);
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Riesegue lo stesso controllo opt-in ogni volta che l'app torna in foreground.
        _ = _updateService.CheckForUpdateIfDueAsync();
    }
}
