using CardMaster.Views;

namespace CardMaster.ViewModels;

/// <summary>
/// Una pastiglia del selettore colore: un colore della palette, oppure l'opzione
/// "Automatico" che mostra in anteprima il colore derivato dal nome corrente.
/// </summary>
public sealed class TileColorOption : ObservableObject
{
    private Color _color;
    private bool _isSelected;

    private TileColorOption(Color color, string? hex, bool isAuto)
    {
        _color = color;
        Hex = hex;
        IsAuto = isAuto;
    }

    /// <summary>Opzione "Automatico": non persiste nessun colore, mostra quello derivato dal nome.</summary>
    public static TileColorOption Auto(Color preview) => new(preview, hex: null, isAuto: true);

    /// <summary>Opzione di palette: persiste il proprio hex in <c>Card.TileColor</c>.</summary>
    public static TileColorOption FromPalette(Color color) => new(color, CardTilePalette.ToHex(color), isAuto: false);

    /// <summary>Hex da persistere; null per l'opzione "Automatico".</summary>
    public string? Hex { get; }

    public bool IsAuto { get; }

    /// <summary>Colore mostrato dalla pastiglia. Per "Automatico" segue il nome della carta.</summary>
    public Color Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
