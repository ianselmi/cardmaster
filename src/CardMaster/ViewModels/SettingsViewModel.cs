using CardMaster.Services;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della pagina Impostazioni: espone la preferenza di tema (persistita e applicata
/// subito) e le informazioni sull'app (nome e versione/build).
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;

    // Etichette mostrate nel Picker, nello stesso ordine dell'enum AppThemePreference.
    private static readonly string[] ThemeLabels = { "Sistema", "Chiaro", "Scuro" };

    public SettingsViewModel(ISettingsStore settings)
    {
        _settings = settings;
    }

    public IReadOnlyList<string> ThemeOptions => ThemeLabels;

    /// <summary>Opzione tema selezionata (etichetta). Get dallo store; set persiste e applica subito.</summary>
    public string SelectedTheme
    {
        get => ThemeLabels[(int)_settings.Theme];
        set
        {
            var index = Array.IndexOf(ThemeLabels, value);
            if (index < 0)
            {
                return;
            }

            var preference = (AppThemePreference)index;
            if (preference == _settings.Theme)
            {
                return;
            }

            _settings.Theme = preference;
            ApplyTheme(preference);
            OnPropertyChanged();
        }
    }

    public string AppName => AppInfo.Current.Name;

    public string AppVersion => $"Versione {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";

    /// <summary>Vero se il backup su Google Drive è attivo, per lo stile del pulsante in Impostazioni.</summary>
    public bool IsBackupEnabled => _settings.BackupEnabled;

    /// <summary>Sottotitolo di stato mostrato sotto il pulsante "Backup su Google Drive".</summary>
    public string BackupStatusText => _settings.BackupEnabled ? "Backup attivo" : "Backup non attivo";

    /// <summary>Applica la preferenza a <see cref="Application.UserAppTheme"/>.</summary>
    public static void ApplyTheme(AppThemePreference preference)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme = preference switch
        {
            AppThemePreference.Light => AppTheme.Light,
            AppThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }
}
