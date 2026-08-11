namespace CardMaster.Services.Receipts;

/// <inheritdoc />
public sealed class ReceiptImageStore : IReceiptImageStore
{
    /// <summary>Sottocartella dei dati dell'app che contiene le immagini degli scontrini.</summary>
    private const string FolderName = "receipts";

    private static string FolderPath => Path.Combine(FileSystem.AppDataDirectory, FolderName);

    public async Task<string?> SaveAsync(
        string sourcePath,
        string receiptId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(FolderPath);

            var extension = Path.GetExtension(sourcePath);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg";
            }

            var fileName = $"{receiptId}{extension}";
            var destination = Path.Combine(FolderPath, fileName);

            await using var source = File.OpenRead(sourcePath);
            await using var target = File.Create(destination);
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);

            // Relativo, non assoluto: il percorso dei dati dell'app cambia tra reinstallazioni
            // e device, e questo valore viaggia nel backup del database.
            return Path.Combine(FolderName, fileName);
        }
        catch (Exception)
        {
            // Non conservare l'immagine non deve impedire di salvare lo scontrino.
            return null;
        }
    }

    public string? ResolveFullPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var full = Path.Combine(FileSystem.AppDataDirectory, relativePath);
        return File.Exists(full) ? full : null;
    }

    public void Delete(string? relativePath)
    {
        var full = ResolveFullPath(relativePath);
        if (full is null)
        {
            return;
        }

        try
        {
            File.Delete(full);
        }
        catch (Exception)
        {
            // Spazio non liberato: fastidioso, non un errore da mostrare all'utente.
        }
    }

    public long GetTotalSizeBytes()
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                return 0;
            }

            return Directory
                .EnumerateFiles(FolderPath)
                .Sum(f => new FileInfo(f).Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public void DeleteAll()
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(FolderPath))
            {
                File.Delete(file);
            }
        }
        catch (Exception)
        {
            // idem: nessun errore da propagare all'utente.
        }
    }
}
