namespace CardMaster.Services.Update;

/// <summary>
/// Installazione dell'APK scaricato tramite il package installer di sistema. Su Android 8+
/// richiede il consenso esplicito "Installa app sconosciute" per l'app.
/// </summary>
public interface IApkInstaller
{
    /// <summary>True se l'app può avviare l'installazione di pacchetti senza ulteriore consenso.</summary>
    bool CanInstallPackages { get; }

    /// <summary>Apre la schermata di sistema per concedere il permesso "Installa app sconosciute".</summary>
    void OpenUnknownSourcesSettings();

    /// <summary>Avvia l'installazione dell'APK indicato tramite l'intent del package installer di sistema.</summary>
    void RequestInstall(string apkFilePath);
}
