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
}
