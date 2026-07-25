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

    /// <summary>Cache locale del limite di quota Drive (byte); null = illimitato o sconosciuto.</summary>
    long? DriveQuotaLimit { get; set; }

    /// <summary>Cache locale dell'uso di quota Drive (byte); null = sconosciuto.</summary>
    long? DriveQuotaUsage { get; set; }
}
