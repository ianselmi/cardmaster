namespace CardMaster.Services.Update;

/// <summary>Versione pubblicata come Release GitHub (tag <c>latest</c>, vedi <c>ci-release</c>).</summary>
public sealed record UpdateRelease(
    string VersionName,
    string ApkUrl,
    long ApkSize,
    string? Sha256);

public enum UpdateCheckOutcome
{
    UpToDate,
    UpdateAvailable,
    Error,
}

public sealed record UpdateCheckResult(
    UpdateCheckOutcome Outcome,
    UpdateRelease? Release = null,
    string? ErrorMessage = null);

public enum UpdateDownloadOutcome
{
    Success,
    ChecksumMismatch,
    NetworkError,
    Cancelled,
}

public sealed record UpdateDownloadResult(
    UpdateDownloadOutcome Outcome,
    string? FilePath = null,
    string? ErrorMessage = null);
