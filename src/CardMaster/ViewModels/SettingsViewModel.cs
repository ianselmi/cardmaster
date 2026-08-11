using CardMaster.Services;
using CardMaster.Services.Backup;
using CardMaster.Services.Receipts;

namespace CardMaster.ViewModels;

/// <summary>Stato mostrato dal pulsante "Backup su Google Drive" nelle Impostazioni.</summary>
public enum BackupTileState
{
    /// <summary>Backup mai abilitato o disattivato.</summary>
    Inactive,

    /// <summary>Backup abilitato e funzionante.</summary>
    Active,

    /// <summary>Backup abilitato ma non funzionante: ultimo tentativo fallito o account da riconnettere.</summary>
    Problem,
}

/// <summary>
/// ViewModel della pagina Impostazioni: espone la preferenza di tema (persistita e applicata
/// subito) e le informazioni sull'app (nome e versione/build).
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly IReceiptImageStore _imageStore;
    private readonly IReceiptRepository _receipts;

    // Etichette mostrate nel Picker, nello stesso ordine dell'enum AppThemePreference.
    private static readonly string[] ThemeLabels = { "Sistema", "Chiaro", "Scuro" };

    public SettingsViewModel(
        ISettingsStore settings,
        IReceiptImageStore imageStore,
        IReceiptRepository receipts)
    {
        _settings = settings;
        _imageStore = imageStore;
        _receipts = receipts;
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

    /// <summary>
    /// Stato del backup per lo stile del pulsante in Impostazioni. Distingue "attivo" da
    /// "attivo ma non funzionante": è ciò che rende percepibile il problema senza entrare
    /// nella sezione dedicata.
    /// </summary>
    public BackupTileState BackupState => !_settings.BackupEnabled
        ? BackupTileState.Inactive
        : _settings.LastBackupError == BackupErrorKind.None
            ? BackupTileState.Active
            : BackupTileState.Problem;

    /// <summary>Sottotitolo di stato mostrato sotto il pulsante "Backup su Google Drive".</summary>
    public string BackupStatusText => BackupState switch
    {
        BackupTileState.Inactive => "Backup non attivo",
        BackupTileState.Active => "Backup attivo",
        _ => _settings.LastBackupError == BackupErrorKind.ReauthRequired
            ? "Backup da riconnettere"
            : "Ultimo backup non riuscito",
    };

    /// <summary>Rilegge lo stato del backup (l'utente può averlo cambiato nella sezione dedicata).</summary>
    public void RefreshBackupState()
    {
        OnPropertyChanged(nameof(BackupState));
        OnPropertyChanged(nameof(BackupStatusText));
    }

    /// <summary>
    /// Conservare l'immagine degli scontrini acquisiti. Spegnendolo si salvano solo dati e
    /// testo riconosciuto: gli scontrini già acquisiti non vengono toccati.
    /// </summary>
    public bool KeepReceiptImages
    {
        get => _settings.KeepReceiptImages;
        set
        {
            if (value == _settings.KeepReceiptImages)
            {
                return;
            }

            _settings.KeepReceiptImages = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Spazio occupato dalle immagini degli scontrini, con il limite del backup dichiarato.</summary>
    public string ReceiptImagesSizeText
    {
        get
        {
            var bytes = _imageStore.GetTotalSizeBytes();
            if (bytes == 0)
            {
                return "Nessuna immagine di scontrino conservata.";
            }

            return $"Le immagini degli scontrini occupano {FormatSize(bytes)}. " +
                   "Non sono comprese nel backup su Drive: eliminandole restano i dati e il testo riconosciuto.";
        }
    }

    public bool HasReceiptImages => _imageStore.GetTotalSizeBytes() > 0;

    /// <summary>
    /// Elimina le immagini conservate e azzera i riferimenti sugli scontrini, così che il
    /// dettaglio spieghi l'assenza invece di puntare a un file che non c'è più.
    /// </summary>
    public async Task ClearReceiptImagesAsync()
    {
        _imageStore.DeleteAll();

        var receipts = await _receipts.GetAllAsync().ConfigureAwait(true);
        foreach (var receipt in receipts.Where(r => r.ImagePath is not null))
        {
            receipt.ImagePath = null;
            await _receipts.UpdateAsync(receipt).ConfigureAwait(true);
        }

        OnPropertyChanged(nameof(ReceiptImagesSizeText));
        OnPropertyChanged(nameof(HasReceiptImages));
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / 1024d / 1024d:0.#} MB",
        >= 1024 => $"{bytes / 1024d:0} KB",
        _ => $"{bytes} byte",
    };

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
