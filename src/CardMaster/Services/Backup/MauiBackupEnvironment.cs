using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace CardMaster.Services.Backup;

/// <summary>Implementazione di <see cref="IBackupEnvironment"/> su MAUI Essentials.</summary>
public sealed class MauiBackupEnvironment : IBackupEnvironment
{
    public string CacheDirectory => FileSystem.CacheDirectory;

    public bool HasNetworkAccess => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
