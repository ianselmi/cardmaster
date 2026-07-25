using Microsoft.Maui.Storage;

namespace CardMaster.Services;

/// <summary>
/// Implementazione di <see cref="ISettingsStore"/> su <see cref="Preferences.Default"/>.
/// Le preferenze sono locali al device e persistono tra i riavvii. I valori enum sono
/// serializzati come stringa per essere robusti a riordini futuri.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private const string ThemeKey = "theme";

    public AppThemePreference Theme
    {
        get
        {
            var raw = Preferences.Default.Get(ThemeKey, nameof(AppThemePreference.System));
            return Enum.TryParse<AppThemePreference>(raw, out var value)
                ? value
                : AppThemePreference.System;
        }
        set => Preferences.Default.Set(ThemeKey, value.ToString());
    }
}
