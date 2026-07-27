using System.Globalization;
using CardMaster.Services;
using CardMaster.Services.Update;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della pagina "Controllo aggiornamenti". Legge/avvia il controllo tramite
/// <see cref="IUpdateService"/> (singleton, sopravvive alla ricreazione della pagina) e osserva il
/// suo stato mentre la pagina è visibile (<see cref="Attach"/>/<see cref="Detach"/>, chiamati da
/// OnAppearing/OnDisappearing). Nessuna eccezione risale alla UI: gli esiti sono messaggi.
/// </summary>
public sealed class UpdateViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;
    private readonly IUpdateDownloadLauncher _launcher;
    private readonly IApkInstaller _installer;
    private readonly ISettingsStore _settings;

    private bool _isChecking;
    private string? _errorMessage;
    private UpdateDownloadResult? _handledDownloadResult;

    public UpdateViewModel(
        IUpdateService updateService,
        IUpdateDownloadLauncher launcher,
        IApkInstaller installer,
        ISettingsStore settings)
    {
        _updateService = updateService;
        _launcher = launcher;
        _installer = installer;
        _settings = settings;

        CheckCommand = new Command(async () => await CheckAsync(), () => !IsBusy);
        DownloadCommand = new Command(async () => await DownloadAsync(), () => !IsBusy && IsUpdateAvailable);
        DismissCommand = new Command(Dismiss, () => IsUpdateAvailable && !IsDismissed);
    }

    public Command CheckCommand { get; }

    public Command DownloadCommand { get; }

    public Command DismissCommand { get; }

    public string InstalledVersion => AppInfo.Current.VersionString;

    public bool IsChecking
    {
        get => _isChecking;
        private set
        {
            if (SetProperty(ref _isChecking, value))
            {
                RaiseStateChanged();
            }
        }
    }

    public bool IsDownloading => _updateService.IsDownloading;

    public bool IsBusy => IsChecking || IsDownloading;

    public bool IsNotBusy => !IsBusy;

    public double DownloadProgress => _updateService.DownloadProgress;

    public int DownloadPercent => (int)Math.Round(DownloadProgress * 100);

    /// <summary>
    /// Versione da installare, già filtrata dal servizio rispetto a quella installata: una volta
    /// installato l'aggiornamento non viene più annunciato, anche senza un nuovo controllo.
    /// </summary>
    public string? AvailableVersion => _updateService.AvailableUpdateVersion;

    public bool IsUpdateAvailable => AvailableVersion is not null;

    /// <summary>Vero se l'utente ha già chiuso il segnale per <see cref="AvailableVersion"/>.</summary>
    public bool IsDismissed => AvailableVersion is not null
        && string.Equals(AvailableVersion, _settings.UpdateNotifyDismissedVersion, StringComparison.Ordinal);

    /// <summary>Opzione "Avvisami di nuove versioni" (controllo automatico opt-in), mostrata come switch in Impostazioni.</summary>
    public bool NotifyEnabled
    {
        get => _settings.UpdateNotifyEnabled;
        set
        {
            if (_settings.UpdateNotifyEnabled != value)
            {
                _settings.UpdateNotifyEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public string LastCheckText
    {
        get
        {
            if (_settings.LastUpdateCheckUtc is not { } utc)
            {
                return "Nessun controllo ancora effettuato";
            }

            var when = utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            return IsUpdateAvailable
                ? $"Ultimo controllo: {when} · versione {AvailableVersion} disponibile"
                : $"Ultimo controllo: {when} · nessun aggiornamento disponibile";
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    /// <summary>Da chiamare in OnAppearing: sincronizza lo stato e inizia a osservare il servizio.</summary>
    public void Attach()
    {
        _updateService.StateChanged += OnUpdateServiceStateChanged;
        RaiseStateChanged();
    }

    /// <summary>Da chiamare in OnDisappearing: smette di osservare il servizio.</summary>
    public void Detach()
    {
        _updateService.StateChanged -= OnUpdateServiceStateChanged;
    }

    private async Task CheckAsync()
    {
        IsChecking = true;
        ErrorMessage = null;
        try
        {
            var result = await _updateService.CheckForUpdateAsync();
            if (result.Outcome == UpdateCheckOutcome.Error)
            {
                ErrorMessage = result.ErrorMessage ?? "Controllo aggiornamenti non riuscito.";
            }
        }
        finally
        {
            IsChecking = false;
        }
    }

    private async Task DownloadAsync()
    {
        if (!_installer.CanInstallPackages)
        {
            var proceed = await ConfirmAsync(
                "Permesso richiesto",
                "Per installare l'aggiornamento devi consentire a CardMaster di installare app sconosciute. Aprire le impostazioni?",
                "Apri impostazioni");
            if (proceed)
            {
                _installer.OpenUnknownSourcesSettings();
            }

            return;
        }

        ErrorMessage = null;
        _launcher.StartDownload();
    }

    private void Dismiss()
    {
        if (AvailableVersion is null)
        {
            return;
        }

        _settings.UpdateNotifyDismissedVersion = AvailableVersion;
        RaiseStateChanged();
    }

    private void OnUpdateServiceStateChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RaiseStateChanged();

            var result = _updateService.LastDownloadResult;
            if (result is not null && !_updateService.IsDownloading && !ReferenceEquals(result, _handledDownloadResult))
            {
                _handledDownloadResult = result;
                HandleDownloadResult(result);
            }
        });
    }

    private void HandleDownloadResult(UpdateDownloadResult result)
    {
        switch (result.Outcome)
        {
            case UpdateDownloadOutcome.Success when result.FilePath is not null:
                _installer.RequestInstall(result.FilePath);
                break;
            case UpdateDownloadOutcome.Cancelled:
                break;
            default:
                ErrorMessage = result.ErrorMessage ?? "Download non riuscito. Riprova.";
                break;
        }
    }

    private void RaiseStateChanged()
    {
        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(DownloadProgress));
        OnPropertyChanged(nameof(DownloadPercent));
        OnPropertyChanged(nameof(AvailableVersion));
        OnPropertyChanged(nameof(IsUpdateAvailable));
        OnPropertyChanged(nameof(IsDismissed));
        OnPropertyChanged(nameof(LastCheckText));
        CheckCommand.ChangeCanExecute();
        DownloadCommand.ChangeCanExecute();
        DismissCommand.ChangeCanExecute();
    }

    private static async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Annulla")
    {
        if (Shell.Current is null)
        {
            return false;
        }

        return await Shell.Current.DisplayAlert(title, message, accept, cancel);
    }
}
