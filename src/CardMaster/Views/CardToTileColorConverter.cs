using System.Globalization;
using CardMaster.Data;

namespace CardMaster.Views;

/// <summary>
/// Converte una carta nel colore di sfondo del suo riquadro, tramite
/// <see cref="CardTilePalette.ForCard"/>. Riceve la carta intera (e non il solo nome)
/// perché la regola "colore scelto dall'utente, altrimenti derivato dal nome" deve
/// stare in un solo punto e non duplicarsi nei DataTemplate.
/// </summary>
public sealed class CardToTileColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => CardTilePalette.ForCard(value as Card);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
