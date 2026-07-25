using Android.Content;
using Android.OS;
using CardMaster.Services.Update;
using AndroidUri = Android.Net.Uri;
using FileProviderCompat = AndroidX.Core.Content.FileProvider;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Avvia l'installazione di un APK tramite il package installer di sistema, esponendo il file
/// scaricato in cache via <see cref="FileProvider"/> (autorità <c>com.cardmaster.app.fileprovider</c>,
/// dichiarata in AndroidManifest.xml). Su Android 8+ richiede il consenso "Installa app sconosciute".
/// </summary>
public sealed class AndroidApkInstaller : IApkInstaller
{
    private const string FileProviderAuthority = "com.cardmaster.app.fileprovider";

    public bool CanInstallPackages
    {
        get
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Permesso "Installa app sconosciute" introdotto in Android 8 (API 26).
                return true;
            }

            var context = global::Android.App.Application.Context;
            return context.PackageManager?.CanRequestPackageInstalls() ?? false;
        }
    }

    public void OpenUnknownSourcesSettings()
    {
        var context = global::Android.App.Application.Context;
        using var intent = new Intent(global::Android.Provider.Settings.ActionManageUnknownAppSources);
        intent.SetData(AndroidUri.Parse($"package:{context.PackageName}"));
        intent.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);
    }

    public void RequestInstall(string apkFilePath)
    {
        var context = global::Android.App.Application.Context;
        var file = new Java.IO.File(apkFilePath);
        var uri = FileProviderCompat.GetUriForFile(context, FileProviderAuthority, file);

        using var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
        context.StartActivity(intent);
    }
}
