using CardMaster.Services;
using Microsoft.Maui.ApplicationModel;

namespace CardMaster.Platforms.Android.Services;

/// <summary>Implementazione Android: agisce sulla luminosità della finestra dell'Activity corrente.</summary>
public sealed class ScreenBrightnessController : IScreenBrightnessController
{
    // 1f = massimo; -1f = default di sistema (BrightnessOverrideNone).
    public void SetMax() => Apply(1f);

    public void RestoreDefault() => Apply(-1f);

    private static void Apply(float brightness)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Platform.CurrentActivity?.Window;
            if (window is null)
            {
                return;
            }

            var layout = window.Attributes;
            if (layout is null)
            {
                return;
            }

            layout.ScreenBrightness = brightness;
            window.Attributes = layout;
        });
    }
}
