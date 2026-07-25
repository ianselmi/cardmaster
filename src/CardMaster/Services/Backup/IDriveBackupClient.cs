namespace CardMaster.Services.Backup;

/// <summary>
/// Client Google Drive v3 (REST su HttpClient grezzo) confinato alla cartella applicativa
/// nascosta (<c>appDataFolder</c>). Ogni chiamata autentica con l'access token corrente e
/// ritenta una volta su 401 dopo un refresh. Nessuna eccezione di rete/servizio deve far
/// crashare l'app: gli errori sono propagati come <see cref="DriveBackupException"/>.
/// </summary>
public interface IDriveBackupClient
{
    /// <summary>Quota dello spazio Drive, o null se non leggibile con lo scope corrente.</summary>
    Task<StorageQuota?> GetStorageQuotaAsync(CancellationToken cancellationToken = default);

    /// <summary>Elenco dei backup nella cartella applicativa, ordinati dal più recente.</summary>
    Task<IReadOnlyList<DriveBackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>Carica un nuovo backup (upload multipart) nella cartella applicativa.</summary>
    Task<DriveBackupFile> UploadBackupAsync(string name, byte[] content, CancellationToken cancellationToken = default);

    /// <summary>Scarica il contenuto del backup indicato su <paramref name="destinationPath"/>.</summary>
    Task DownloadBackupAsync(string fileId, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>Elimina il backup indicato dalla cartella applicativa.</summary>
    Task DeleteBackupAsync(string fileId, CancellationToken cancellationToken = default);
}

/// <summary>Errore controllato di un'operazione Drive (rete, servizio o autenticazione).</summary>
public sealed class DriveBackupException : Exception
{
    public DriveBackupException(string message, Exception? inner = null) : base(message, inner)
    {
    }

    /// <summary>True quando le credenziali non sono più valide e serve ri-autenticarsi.</summary>
    public bool RequiresReauth { get; init; }
}
