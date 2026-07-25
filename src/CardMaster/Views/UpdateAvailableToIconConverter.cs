using System.Globalization;

namespace CardMaster.Views;

/// <summary>
/// Sceglie l'icona del pulsante Impostazioni: con badge (pallino rosso) se un aggiornamento
/// è disponibile e non ancora silenziato, altrimenti l'icona normale.
/// </summary>
public sealed class UpdateAvailableToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "settings_alert.png" : "settings.png";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
