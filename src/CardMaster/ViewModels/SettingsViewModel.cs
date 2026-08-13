using CardMaster.Services;
using CardMaster.Services.Ai;
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
    private readonly IAiCredentialStore _aiCredentials;
    private readonly IAiKeyVerifier _aiKeyVerifier;

    // Etichette mostrate nel Picker, nello stesso ordine dell'enum AppThemePreference.
    private static readonly string[] ThemeLabels = { "Sistema", "Chiaro", "Scuro" };

    public SettingsViewModel(
        ISettingsStore settings,
        IReceiptImageStore imageStore,
        IReceiptRepository receipts,
        IAiCredentialStore aiCredentials,
        IAiKeyVerifier aiKeyVerifier)
    {
        _settings = settings;
        _imageStore = imageStore;
        _receipts = receipts;
        _aiCredentials = aiCredentials;
        _aiKeyVerifier = aiKeyVerifier;
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

    // ---- Lettura assistita degli scontrini -------------------------------------------------

    /// <summary>
    /// Interruttore della funzione. <b>Spento per default</b>, ed è l'unico stato in cui nessun
    /// dato dello scontrino può lasciare il device. Accenderlo non basta: serve anche la chiave,
    /// e l'invio resta comunque una scelta per singolo scontrino.
    /// </summary>
    public bool AiScanEnabled
    {
        get => _settings.AiScanEnabled;
        set
        {
            if (value == _settings.AiScanEnabled)
            {
                return;
            }

            _settings.AiScanEnabled = value;
            OnPropertyChanged();
            RefreshAiState();
        }
    }

    /// <summary>Se una chiave risulta configurata. Il valore non è rileggibile: solo il fatto.</summary>
    public bool AiKeyConfigured => _aiCredentials.IsConfigured;

    /// <summary>
    /// Stato a colpo d'occhio: spenta, attiva ma inutilizzabile, oppure pronta. La distinzione
    /// serve perché "attiva senza chiave" sembra funzionante e non lo è.
    /// </summary>
    public string AiStatusText => !_settings.AiScanEnabled
        ? "Lettura assistita spenta. Nessun dato degli scontrini lascia il telefono."
        : _aiCredentials.IsConfigured
            ? "Lettura assistita attiva e pronta."
            : "Lettura assistita attiva, ma senza chiave non può funzionare: inseriscine una.";

    /// <summary>
    /// Che cosa lascia il device quando la funzione è attiva. Sta nelle impostazioni, non solo
    /// nel momento dell'invio: chi accende deve poterlo leggere prima e rileggere dopo.
    /// </summary>
    public string AiDisclosureText =>
        "Con la funzione attiva, e solo quando lo chiedi per un singolo scontrino che non torna, " +
        "la foto di quello scontrino — prodotti, prezzi, esercente e data — viene inviata all'API " +
        "di Anthropic usando la tua chiave e a tue spese. Su uno scontrino che quadra non parte " +
        "nessuna chiamata. La chiave resta nell'archivio protetto del telefono: non è nel backup " +
        "su Drive, non è nel database, e non è leggibile da qui dopo l'inserimento.";

    /// <summary>Modelli selezionabili, ciascuno con il costo indicativo per scontrino accanto.</summary>
    public IReadOnlyList<string> AiModelOptions { get; } =
        ReceiptAiModels.All
            .Select(m => $"{m.DisplayName} — circa {FormatMicroCents(ReceiptAiModels.EstimatedCostMicroCents(m))} a scontrino")
            .ToList();

    public string SelectedAiModel
    {
        get
        {
            var index = ReceiptAiModels.All.ToList()
                .FindIndex(m => m.Id == ReceiptAiModels.Resolve(_settings.AiScanModelId).Id);
            return AiModelOptions[Math.Max(0, index)];
        }
        set
        {
            var index = AiModelOptions.ToList().IndexOf(value);
            if (index < 0 || ReceiptAiModels.All[index].Id == _settings.AiScanModelId)
            {
                return;
            }

            _settings.AiScanModelId = ReceiptAiModels.All[index].Id;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Costo <b>effettivo</b> dell'ultima rilettura, ricavato dal consumo riportato dalla
    /// risposta. È il controllo della stima: se le due divergono, la stima è da rivedere.
    /// </summary>
    public string AiLastCostText
    {
        get
        {
            if (_settings.LastAiScanCostMicroCents is not { } cost)
            {
                return "Nessuna rilettura ancora effettuata.";
            }

            var input = _settings.LastAiScanInputTokens ?? 0;
            var output = _settings.LastAiScanOutputTokens ?? 0;
            return $"Ultima rilettura: {FormatMicroCents(cost)} ({input} token in ingresso, {output} in uscita).";
        }
    }

    /// <summary>
    /// Conserva la chiave digitata. Non la verifica: la verifica è un'azione separata, così
    /// l'utente può salvarla anche senza rete e provarla dopo.
    /// </summary>
    public async Task<bool> SetAiKeyAsync(string apiKey)
    {
        var saved = await _aiCredentials.SetKeyAsync(apiKey).ConfigureAwait(true);
        RefreshAiState();
        return saved;
    }

    /// <summary>
    /// Rimuove la chiave. Le funzioni che la richiedono tornano indisponibili; gli scontrini
    /// già salvati non vengono toccati.
    /// </summary>
    public async Task RemoveAiKeyAsync()
    {
        await _aiCredentials.RemoveKeyAsync().ConfigureAwait(true);
        RefreshAiState();
    }

    /// <summary>
    /// Prova la chiave conservata e restituisce un messaggio comprensibile. Distingue una chiave
    /// rifiutata da un problema di rete: nel secondo caso non sappiamo se la chiave sia buona.
    /// </summary>
    public async Task<string> VerifyAiKeyAsync()
    {
        var key = await _aiCredentials.GetKeyAsync().ConfigureAwait(true);
        if (string.IsNullOrEmpty(key))
        {
            RefreshAiState();
            return "Nessuna chiave configurata.";
        }

        var result = await _aiKeyVerifier.VerifyAsync(key).ConfigureAwait(true);
        return result.Valid
            ? "Chiave valida: il servizio l'ha accettata."
            : result.Error switch
            {
                AiErrorKind.KeyRejected => "Chiave rifiutata dal servizio. Controlla di averla incollata per intero.",
                AiErrorKind.CreditExhausted => "La chiave è valida, ma il credito dell'account è esaurito.",
                AiErrorKind.RateLimited => "Troppe richieste in poco tempo. Riprova tra qualche minuto.",
                AiErrorKind.Network => "Nessuna connessione: non è stato possibile verificarla. La chiave resta salvata.",
                AiErrorKind.Timeout => "Il servizio non ha risposto in tempo. Riprova.",
                _ => "Verifica non riuscita. Riprova più tardi.",
            };
    }

    private void RefreshAiState()
    {
        OnPropertyChanged(nameof(AiKeyConfigured));
        OnPropertyChanged(nameof(AiStatusText));
    }

    /// <summary>Ricarica lo stato della lettura assistita quando la pagina torna in primo piano.</summary>
    public void RefreshAiSection()
    {
        RefreshAiState();
        OnPropertyChanged(nameof(AiScanEnabled));
        OnPropertyChanged(nameof(AiLastCostText));
    }

    /// <summary>
    /// Millesimi di centesimo in un importo leggibile. Sotto il centesimo si scrive comunque il
    /// valore invece di arrotondare a zero: "0 ¢" farebbe sembrare la funzione gratuita.
    /// </summary>
    private static string FormatMicroCents(long microCents) =>
        $"{microCents / 1000m:0.###} ¢".Replace('.', ',');

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
