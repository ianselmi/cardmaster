using System.Collections.ObjectModel;
using System.Globalization;
using CardMaster.Services.Backup;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della sezione "Backup su Google Drive". Stato disabilitato di default; in stato
/// abilitato espone account, ultimo backup, spazio e frequenza, con le azioni di backup,
/// ripristino e disconnessione. I valori sono letti dalla cache offline e aggiornati quando
/// c'è rete; nessuna eccezione risale alla UI (gli esiti sono messaggi).
/// </summary>
public sealed class BackupViewModel : ObservableObject
{
    private static readonly (BackupFrequency Value, string Label)[] Frequencies =
    {
        (BackupFrequency.Never, "Mai"),
        (BackupFrequency.OnEachOpen, "A ogni apertura"),
        (BackupFrequency.Daily, "Giornaliero"),
        (BackupFrequency.Weekly, "Settimanale"),
    };

    private readonly IBackupService _backup;

    private bool _isBusy;

    public BackupViewModel(IBackupService backup)
    {
        _backup = backup;

        EnableCommand = new Command(async () => await EnableAsync(), () => !IsBusy);
        DisableCommand = new Command(async () => await DisableAsync(), () => !IsBusy);
        BackupNowCommand = new Command(async () => await BackupNowAsync(), () => !IsBusy);
        LoadRestoreListCommand = new Command(async () => await LoadRestoreListAsync(), () => !IsBusy);
        RestoreCommand = new Command<BackupListItem>(async item => await RestoreAsync(item), _ => !IsBusy);
    }

    public Command EnableCommand { get; }

    public Command DisableCommand { get; }

    public Command BackupNowCommand { get; }

    public Command LoadRestoreListCommand { get; }

    public Command<BackupListItem> RestoreCommand { get; }

    public ObservableCollection<BackupListItem> Backups { get; } = new();

    public IReadOnlyList<string> FrequencyOptions { get; } = Frequencies.Select(f => f.Label).ToList();

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                RaiseCommandStates();
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public bool IsEnabled => _backup.IsEnabled;

    public bool IsDisabled => !_backup.IsEnabled;

    public string? AccountEmail => _backup.AccountEmail;

    public string LastBackupText
    {
        get
        {
            if (_backup.LastBackupUtc is not { } utc)
            {
                return "Nessun backup ancora eseguito";
            }

            var when = utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            return _backup.LastBackupSize is { } size
                ? $"{when} · {FormatBytes(size)}"
                : when;
        }
    }

    public string QuotaText
    {
        get
        {
            if (_backup.CachedQuota is not { } quota)
            {
                return "Spazio Drive: non disponibile";
            }

            return quota.Limit is { } limit
                ? $"Spazio Drive: {FormatBytes(quota.Usage)} / {FormatBytes(limit)}"
                : "Spazio Drive: illimitato";
        }
    }

    public string SelectedFrequency
    {
        get => LabelFor(_backup.Frequency);
        set
        {
            var match = Frequencies.FirstOrDefault(f => f.Label == value);
            if (match.Label is null || match.Value == _backup.Frequency)
            {
                return;
            }

            _ = _backup.SetFrequencyAsync(match.Value);
            OnPropertyChanged();
        }
    }

    /// <summary>Ricarica lo stato mostrato (chiamata all'apparire della pagina) e aggiorna la quota se c'è rete.</summary>
    public async Task RefreshAsync()
    {
        RaiseStateChanged();

        if (_backup.IsEnabled)
        {
            await _backup.RefreshQuotaAsync().ConfigureAwait(true);
            OnPropertyChanged(nameof(QuotaText));
        }
    }

    private async Task EnableAsync()
    {
        await RunBusyAsync(async () =>
        {
            // POST_NOTIFICATIONS (Android 13+): richiesto in fase di abilitazione del backup.
            await Permissions.RequestAsync<Permissions.PostNotifications>();

            var ok = await _backup.EnableAsync();
            if (!ok)
            {
                await AlertAsync("Backup", "Accesso a Google non completato. Il backup resta disabilitato.");
            }
        });

        RaiseStateChanged();
    }

    private async Task DisableAsync()
    {
        var confirm = await ConfirmAsync(
            "Disabilita backup",
            "Verrà scollegato l'account e annullata la schedulazione. I backup già su Drive restano disponibili.",
            "Disabilita");
        if (!confirm)
        {
            return;
        }

        await RunBusyAsync(async () => await _backup.DisableAsync());
        Backups.Clear();
        RaiseStateChanged();
    }

    private async Task BackupNowAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _backup.BackupNowAsync();
            await AlertAsync(
                "Backup",
                result.Success ? "Backup completato con successo." : $"Backup non riuscito. {result.ErrorMessage}");
        });

        OnPropertyChanged(nameof(LastBackupText));
        OnPropertyChanged(nameof(QuotaText));
    }

    private async Task LoadRestoreListAsync()
    {
        await RunBusyAsync(async () =>
        {
            Backups.Clear();
            try
            {
                var files = await _backup.ListBackupsAsync();
                foreach (var file in files)
                {
                    Backups.Add(new BackupListItem(
                        file.Id,
                        file.ModifiedTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture),
                        FormatBytes(file.Size)));
                }

                if (Backups.Count == 0)
                {
                    await AlertAsync("Ripristina", "Nessun backup disponibile su Drive.");
                }
            }
            catch (DriveBackupException ex)
            {
                await AlertAsync("Ripristina", $"Impossibile leggere i backup. {ex.Message}");
            }
        });
    }

    private async Task RestoreAsync(BackupListItem? item)
    {
        if (item is null)
        {
            return;
        }

        var confirm = await ConfirmAsync(
            "Ripristina backup",
            $"Il database attuale verrà SOSTITUITO con il backup del {item.DateText}. Operazione irreversibile (verrà comunque creata una copia di sicurezza locale). Continuare?",
            "Ripristina");
        if (!confirm)
        {
            return;
        }

        RestoreResult result = new(RestoreOutcome.Failed);
        await RunBusyAsync(async () => result = await _backup.RestoreAsync(item.Id));

        switch (result.Outcome)
        {
            case RestoreOutcome.Success:
                var undo = await ConfirmAsync(
                    "Ripristino completato",
                    "I dati sono stati ripristinati. Vuoi annullare e tornare allo stato precedente?",
                    "Annulla ripristino",
                    "Mantieni");
                if (undo)
                {
                    await RunBusyAsync(async () => await _backup.UndoLastRestoreAsync());
                }

                break;
            case RestoreOutcome.SchemaTooNew:
                await AlertAsync("Ripristina", "Questo backup è stato creato da una versione più recente dell'app. Aggiorna l'app per ripristinarlo.");
                break;
            case RestoreOutcome.NotFound:
                await AlertAsync("Ripristina", "Backup non più disponibile.");
                break;
            default:
                await AlertAsync("Ripristina", $"Ripristino non riuscito. {result.ErrorMessage}");
                break;
        }
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await action();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseStateChanged()
    {
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(IsDisabled));
        OnPropertyChanged(nameof(AccountEmail));
        OnPropertyChanged(nameof(LastBackupText));
        OnPropertyChanged(nameof(QuotaText));
        OnPropertyChanged(nameof(SelectedFrequency));
    }

    private void RaiseCommandStates()
    {
        EnableCommand.ChangeCanExecute();
        DisableCommand.ChangeCanExecute();
        BackupNowCommand.ChangeCanExecute();
        LoadRestoreListCommand.ChangeCanExecute();
        RestoreCommand.ChangeCanExecute();
    }

    private static string LabelFor(BackupFrequency frequency) =>
        Frequencies.First(f => f.Value == frequency).Label;

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value.ToString(unit == 0 ? "0" : "0.#", CultureInfo.CurrentCulture)} {units[unit]}";
    }

    private static Task AlertAsync(string title, string message) =>
        Shell.Current?.DisplayAlert(title, message, "OK") ?? Task.CompletedTask;

    private static async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Annulla")
    {
        if (Shell.Current is null)
        {
            return false;
        }

        return await Shell.Current.DisplayAlert(title, message, accept, cancel);
    }
}

/// <summary>Elemento della lista di ripristino (backup mostrato in-app).</summary>
public sealed record BackupListItem(string Id, string DateText, string SizeText);
