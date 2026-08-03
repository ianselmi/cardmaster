using System.Globalization;
using CardMaster.ViewModels;

namespace CardMaster.Views;

/// <summary>
/// Colore di sfondo del pulsante "Backup su Google Drive" in Impostazioni: acceso quando il
/// backup è attivo, neutro quando non lo è, rosso quando è attivo ma non funziona (ultimo
/// tentativo fallito o account da riconnettere). Riusa le tinte già presenti nell'app —
/// il rosso è lo stesso del pulsante "Disabilita backup" in BackupPage.
/// </summary>
public sealed class BackupStatusToColorConverter : IValueConverter
{
    private static readonly Color Active = Color.FromArgb("#F59E0B");
    private static readonly Color Inactive = Color.FromArgb("#919191");
    private static readonly Color Problem = Color.FromArgb("#B00020");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            BackupTileState.Active => Active,
            BackupTileState.Problem => Problem,
            _ => Inactive,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
