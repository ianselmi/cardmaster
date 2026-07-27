using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using CardMaster.Platforms.Android.Services;

namespace CardMaster;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleUpdateFlowIntent(Intent);
    }

    /// <summary>
    /// L'Activity è SingleTop: se l'app è già viva, il tocco sulla notifica non passa da
    /// OnCreate ma da qui. Senza questo override la notifica riporterebbe l'utente alla
    /// lista carte invece che al flusso di aggiornamento.
    /// </summary>
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleUpdateFlowIntent(intent);
    }

    private static void HandleUpdateFlowIntent(Intent? intent)
    {
        if (intent?.GetBooleanExtra(AndroidUpdateNotifier.OpenUpdateFlowExtra, false) != true)
        {
            return;
        }

        // Consuma l'extra: una rotazione o un ritorno da background non deve riaprire la pagina.
        intent.RemoveExtra(AndroidUpdateNotifier.OpenUpdateFlowExtra);

        // La Shell non esiste ancora quando l'app viene avviata da fredda: si naviga appena
        // il framework ha creato la finestra.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            for (var attempt = 0; attempt < 20 && Shell.Current is null; attempt++)
            {
                await Task.Delay(100);
            }

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("UpdatePage");
            }
        });
    }
}
