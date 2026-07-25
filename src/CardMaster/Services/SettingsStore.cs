using CardMaster.Services.Backup;
using Microsoft.Maui.Storage;

namespace CardMaster.Services;

/// <summary>
/// Implementazione di <see cref="ISettingsStore"/> su <see cref="Preferences.Default"/>.
/// Le preferenze sono locali al device e persistono tra i riavvii. I valori enum sono
/// serializzati come stringa per essere robusti a riordini futuri.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private const string ThemeKey = "theme";
    private const string BackupEnabledKey = "backup_enabled";
    private const string BackupFrequencyKey = "backup_frequency";
    private const string LastBackupUtcKey = "backup_last_utc";
    private const string LastBackupSizeKey = "backup_last_size";
    private const string DriveQuotaLimitKey = "backup_quota_limit";
    private const string DriveQuotaUsageKey = "backup_quota_usage";
    private const string LastUpdateCheckUtcKey = "update_last_check_utc";
    private const string LastUpdateCheckAvailableVersionKey = "update_last_check_available_version";
    private const string UpdateNotifyEnabledKey = "update_notify_enabled";
    private const string UpdateNotifyDismissedVersionKey = "update_notify_dismissed_version";

    public AppThemePreference Theme
    {
        get
        {
            var raw = Preferences.Default.Get(ThemeKey, nameof(AppThemePreference.System));
            return Enum.TryParse<AppThemePreference>(raw, out var value)
                ? value
                : AppThemePreference.System;
        }
        set => Preferences.Default.Set(ThemeKey, value.ToString());
    }

    public bool BackupEnabled
    {
        get => Preferences.Default.Get(BackupEnabledKey, false);
        set => Preferences.Default.Set(BackupEnabledKey, value);
    }

    public BackupFrequency BackupFrequency
    {
        get
        {
            var raw = Preferences.Default.Get(BackupFrequencyKey, nameof(BackupFrequency.Never));
            return Enum.TryParse<BackupFrequency>(raw, out var value)
                ? value
                : BackupFrequency.Never;
        }
        set => Preferences.Default.Set(BackupFrequencyKey, value.ToString());
    }

    public DateTimeOffset? LastBackupUtc
    {
        get
        {
            var ticks = Preferences.Default.Get(LastBackupUtcKey, 0L);
            return ticks == 0L ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
        set
        {
            if (value is null)
            {
                Preferences.Default.Remove(LastBackupUtcKey);
            }
            else
            {
                Preferences.Default.Set(LastBackupUtcKey, value.Value.UtcTicks);
            }
        }
    }

    public long? LastBackupSize
    {
        get => GetNullableLong(LastBackupSizeKey);
        set => SetNullableLong(LastBackupSizeKey, value);
    }

    public long? DriveQuotaLimit
    {
        get => GetNullableLong(DriveQuotaLimitKey);
        set => SetNullableLong(DriveQuotaLimitKey, value);
    }

    public long? DriveQuotaUsage
    {
        get => GetNullableLong(DriveQuotaUsageKey);
        set => SetNullableLong(DriveQuotaUsageKey, value);
    }

    public DateTimeOffset? LastUpdateCheckUtc
    {
        get
        {
            var ticks = Preferences.Default.Get(LastUpdateCheckUtcKey, 0L);
            return ticks == 0L ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
        set
        {
            if (value is null)
            {
                Preferences.Default.Remove(LastUpdateCheckUtcKey);
            }
            else
            {
                Preferences.Default.Set(LastUpdateCheckUtcKey, value.Value.UtcTicks);
            }
        }
    }

    public string? LastUpdateCheckAvailableVersion
    {
        get => Preferences.Default.Get(LastUpdateCheckAvailableVersionKey, (string?)null);
        set
        {
            if (value is null)
            {
                Preferences.Default.Remove(LastUpdateCheckAvailableVersionKey);
            }
            else
            {
                Preferences.Default.Set(LastUpdateCheckAvailableVersionKey, value);
            }
        }
    }

    public bool UpdateNotifyEnabled
    {
        get => Preferences.Default.Get(UpdateNotifyEnabledKey, false);
        set => Preferences.Default.Set(UpdateNotifyEnabledKey, value);
    }

    public string? UpdateNotifyDismissedVersion
    {
        get => Preferences.Default.Get(UpdateNotifyDismissedVersionKey, (string?)null);
        set
        {
            if (value is null)
            {
                Preferences.Default.Remove(UpdateNotifyDismissedVersionKey);
            }
            else
            {
                Preferences.Default.Set(UpdateNotifyDismissedVersionKey, value);
            }
        }
    }

    private static long? GetNullableLong(string key) =>
        Preferences.Default.ContainsKey(key) ? Preferences.Default.Get(key, 0L) : null;

    private static void SetNullableLong(string key, long? value)
    {
        if (value is null)
        {
            Preferences.Default.Remove(key);
        }
        else
        {
            Preferences.Default.Set(key, value.Value);
        }
    }
}
