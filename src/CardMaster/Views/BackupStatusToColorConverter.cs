using System.Globalization;

namespace CardMaster.Views;

/// <summary>
/// Colore di sfondo del pulsante "Backup su Google Drive" in Impostazioni: più acceso quando il
/// backup è attivo, neutro quando non lo è — riusa le tinte già presenti in Colors.xaml
/// (PrimaryDark/Gray400) invece di introdurne di nuove.
/// </summary>
public sealed class BackupStatusToColorConverter : IValueConverter
{
    private static readonly Color Active = Color.FromArgb("#F59E0B");
    private static readonly Color Inactive = Color.FromArgb("#919191");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Active : Inactive;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
