using Android.Provider;
using CardMaster.Services;
using Microsoft.Maui.ApplicationModel;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Implementazione Android: legge l'impostazione AOSP <c>night_display_activated</c>.
/// Best-effort: se la chiave non esiste (OEM diversi) restituisce Unknown, senza falsi allarmi.
/// </summary>
public sealed class ReadingFilterProbe : IReadingFilterProbe
{
    private const string NightDisplayActivated = "night_display_activated";

    public ReadingFilterState Probe()
    {
        try
        {
            var resolver = Platform.AppContext.ContentResolver;
            if (resolver is null)
            {
                return ReadingFilterState.Unknown;
            }

            // Overload senza default: lancia SettingNotFoundException se la chiave non c'è.
            var value = Settings.Secure.GetInt(resolver, NightDisplayActivated);
            return value == 1 ? ReadingFilterState.Active : ReadingFilterState.Inactive;
        }
        catch (Settings.SettingNotFoundException)
        {
            return ReadingFilterState.Unknown;
        }
        catch
        {
            return ReadingFilterState.Unknown;
        }
    }
}
