using System.Globalization;

namespace CardMaster.Services.Backup;

/// <summary>
/// Convenzione di naming dei file di backup e logiche pure derivate: parsing della
/// versione di schema (guardia anti-downgrade) e selezione dei backup in eccesso per la
/// ritenzione. Nessuna dipendenza da MAUI/Android: interamente testabile in isolamento.
///
/// Nome file: <c>cardmaster-&lt;timestampUtc&gt;-v&lt;schemaVersion&gt;.db3</c>,
/// es. <c>cardmaster-20260725T140501Z-v1.db3</c>.
/// </summary>
public static class BackupNaming
{
    public const string Prefix = "cardmaster-";
    public const string Extension = ".db3";
    private const string TimestampFormat = "yyyyMMddTHHmmssZ";

    public static int MaxRetained => 3;

    public static string Build(DateTimeOffset timestampUtc, int schemaVersion) =>
        $"{Prefix}{timestampUtc.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)}-v{schemaVersion}{Extension}";

    /// <summary>
    /// Estrae timestamp e versione di schema dal nome file. Restituisce false se il nome
    /// non segue la convenzione (in tal caso non è un backup gestito da questa app).
    /// </summary>
    public static bool TryParse(string? name, out DateTimeOffset timestampUtc, out int schemaVersion)
    {
        timestampUtc = default;
        schemaVersion = 0;

        if (string.IsNullOrEmpty(name)
            || !name.StartsWith(Prefix, StringComparison.Ordinal)
            || !name.EndsWith(Extension, StringComparison.Ordinal))
        {
            return false;
        }

        var core = name[Prefix.Length..^Extension.Length];
        var dash = core.LastIndexOf("-v", StringComparison.Ordinal);
        if (dash <= 0)
        {
            return false;
        }

        var timestampText = core[..dash];
        var versionText = core[(dash + 2)..];

        if (!int.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out schemaVersion))
        {
            return false;
        }

        if (!DateTimeOffset.TryParseExact(
                timestampText, TimestampFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestampUtc))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// True se un backup con lo schema indicato può essere ripristinato dall'app che
    /// supporta <paramref name="supportedSchemaVersion"/>. È un downgrade vietato quando lo
    /// schema del backup è più recente di quello supportato.
    /// </summary>
    public static bool CanRestore(int backupSchemaVersion, int supportedSchemaVersion) =>
        backupSchemaVersion <= supportedSchemaVersion;

    /// <summary>
    /// Dato l'insieme corrente di backup, restituisce quelli da eliminare per rientrare nel
    /// limite di ritenzione (i più vecchi oltre <see cref="MaxRetained"/>), ordinando per
    /// data di modifica decrescente.
    /// </summary>
    public static IReadOnlyList<DriveBackupFile> SelectExcess(IEnumerable<DriveBackupFile> backups, int keep = 3)
    {
        return backups
            .OrderByDescending(b => b.ModifiedTime)
            .Skip(keep)
            .ToList();
    }
}
