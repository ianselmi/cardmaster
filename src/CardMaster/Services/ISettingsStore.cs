using CardMaster.Services.Backup;

namespace CardMaster.Services;

/// <summary>
/// Preferenza di tema scelta dall'utente. Mappata su <see cref="Microsoft.Maui.ApplicationModel.AppTheme"/>:
/// System → Unspecified (segue il sistema), Light → Light, Dark → Dark.
/// </summary>
public enum AppThemePreference
{
    System,
    Light,
    Dark,
}

/// <summary>
/// Porta unica per leggere/scrivere le preferenze dell'app come coppie chiave/valore
/// locali al device (nessuna rete, nessun account). Incapsula l'API MAUI Preferences.
/// Estendibile: le change future (es. backup) aggiungono proprietà senza rompere questa.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Tema dell'app. Default <see cref="AppThemePreference.System"/> se mai impostato.</summary>
    AppThemePreference Theme { get; set; }

    /// <summary>Backup su Google Drive abilitato. Default false (opt-in).</summary>
    bool BackupEnabled { get; set; }

    /// <summary>Frequenza del backup automatico. Default <see cref="BackupFrequency.Never"/>.</summary>
    BackupFrequency BackupFrequency { get; set; }

    /// <summary>Cache locale del timestamp UTC dell'ultimo backup riuscito (per la UI offline).</summary>
    DateTimeOffset? LastBackupUtc { get; set; }

    /// <summary>Cache locale della dimensione (byte) dell'ultimo backup riuscito.</summary>
    long? LastBackupSize { get; set; }

    /// <summary>
    /// Timestamp UTC dell'ultimo <b>tentativo</b> di backup, riuscito o fallito; null se mai
    /// tentato. Da non confondere con <see cref="LastBackupUtc"/>, che è l'ultimo successo.
    /// </summary>
    DateTimeOffset? LastBackupAttemptUtc { get; set; }

    /// <summary>
    /// Categoria dell'errore dell'ultimo tentativo di backup; <see cref="BackupErrorKind.None"/>
    /// se l'ultimo tentativo è riuscito o non è mai stato eseguito.
    /// </summary>
    BackupErrorKind LastBackupError { get; set; }

    /// <summary>Cache locale del limite di quota Drive (byte); null = illimitato o sconosciuto.</summary>
    long? DriveQuotaLimit { get; set; }

    /// <summary>Cache locale dell'uso di quota Drive (byte); null = sconosciuto.</summary>
    long? DriveQuotaUsage { get; set; }

    /// <summary>Timestamp UTC dell'ultimo controllo aggiornamenti eseguito; null se mai eseguito.</summary>
    DateTimeOffset? LastUpdateCheckUtc { get; set; }

    /// <summary>Versione rilevata come disponibile all'ultimo controllo aggiornamenti; null se nessun aggiornamento o mai controllato.</summary>
    string? LastUpdateCheckAvailableVersion { get; set; }

    /// <summary>Controllo automatico degli aggiornamenti abilitato dall'utente. Default false (opt-in).</summary>
    bool UpdateNotifyEnabled { get; set; }

    /// <summary>Versione remota per cui l'utente ha già chiuso il segnale di aggiornamento; null se nessuna.</summary>
    string? UpdateNotifyDismissedVersion { get; set; }

    /// <summary>
    /// Conservare l'immagine degli scontrini acquisiti. Default true: l'immagine serve a
    /// verificare una cifra dubbia. Disattivandola si salvano solo dati e testo riconosciuto.
    /// </summary>
    bool KeepReceiptImages { get; set; }

    /// <summary>
    /// Rilettura degli scontrini tramite modello. Default false: la funzione nasce spenta, ed è
    /// l'unico stato in cui nessun dato dello scontrino può lasciare il device.
    /// </summary>
    bool AiScanEnabled { get; set; }

    /// <summary>Identificativo del modello scelto. Default <c>claude-opus-5</c>.</summary>
    string AiScanModelId { get; set; }

    /// <summary>Token in ingresso dell'ultima rilettura; null se mai eseguita.</summary>
    long? LastAiScanInputTokens { get; set; }

    /// <summary>Token in uscita dell'ultima rilettura; null se mai eseguita.</summary>
    long? LastAiScanOutputTokens { get; set; }

    /// <summary>
    /// Costo effettivo dell'ultima rilettura in millesimi di centesimo, ricavato dal consumo
    /// riportato dalla risposta — non dalla stima. Null se mai eseguita.
    /// </summary>
    long? LastAiScanCostMicroCents { get; set; }

    /// <summary>
    /// Scatta quando il consumo dell'ultima rilettura cambia.
    /// <para>
    /// Serve perché chi <b>scrive</b> il dato (la schermata dello scontrino) e chi lo
    /// <b>mostra</b> (le Impostazioni) sono due pagine diverse, e la seconda può essere già
    /// aperta e viva su un'altra tab mentre la prima lavora: tornandoci non passa da
    /// <c>OnAppearing</c> e mostrerebbe un costo vecchio. Verificato su emulatore il 13 ago 2026.
    /// </para>
    /// L'implementazione usa un <see cref="Microsoft.Maui.WeakEventManager"/>: lo store è un
    /// singleton e i ViewModel sono transienti, quindi una sottoscrizione forte li terrebbe in
    /// vita per sempre.
    /// </summary>
    event EventHandler AiUsageChanged;
}
