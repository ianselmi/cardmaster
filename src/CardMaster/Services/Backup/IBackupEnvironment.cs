namespace CardMaster.Services.Backup;

/// <summary>
/// Astrazione delle dipendenze MAUI Essentials usate da <see cref="BackupService"/> (cartella
/// cache, stato di rete), per renderlo testabile senza un host MAUI reale.
/// </summary>
public interface IBackupEnvironment
{
    /// <summary>Cartella cache app-privata per i file temporanei di snapshot/download.</summary>
    string CacheDirectory { get; }

    /// <summary>True se il device ha accesso a Internet in questo momento.</summary>
    bool HasNetworkAccess { get; }
}
