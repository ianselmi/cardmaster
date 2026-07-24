using System.Globalization;

namespace CardMaster.Views;

/// <summary>
/// Converte il nome di una carta nel colore di sfondo del suo riquadro,
/// tramite <see cref="CardTilePalette"/>.
/// </summary>
public sealed class NameToTileColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => CardTilePalette.ForName(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
