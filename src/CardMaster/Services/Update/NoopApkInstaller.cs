namespace CardMaster.Services.Update;

/// <summary>Installer no-op per le piattaforme senza package installer Android (fallback non-Android).</summary>
public sealed class NoopApkInstaller : IApkInstaller
{
    public bool CanInstallPackages => false;

    public void OpenUnknownSourcesSettings()
    {
    }

    public void RequestInstall(string apkFilePath)
    {
    }
}
