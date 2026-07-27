using CardMaster.Data;
using Microsoft.Maui.Graphics;

namespace CardMaster.Views;

/// <summary>
/// Palette curata per i riquadri della lista carte. Colori mid/saturi pensati per
/// essere leggibili con testo bianco. Il colore di una carta è quello scelto dall'utente
/// quando c'è; altrimenti è derivato in modo deterministico dal nome (stessa carta →
/// stesso colore). È la stessa palette in entrambi i casi, così la lista resta coerente.
/// </summary>
public static class CardTilePalette
{
    // Palette allineata al brand ambra/arancio (change maui-restyle). Toni scuri/saturi
    // per leggere bene con testo bianco. Nessun tile coincide con l'ambra dei controlli
    // (Primary #E07B1A), per non confondere colore-azione e colore-carta.
    private static readonly Color[] Palette =
    {
        Color.FromArgb("#C2410C"), // arancio bruciato
        Color.FromArgb("#B45309"), // ambra scuro
        Color.FromArgb("#0F766E"), // teal
        Color.FromArgb("#1D4ED8"), // blu
        Color.FromArgb("#15803D"), // verde
        Color.FromArgb("#7C3AED"), // viola
        Color.FromArgb("#BE185D"), // magenta
        Color.FromArgb("#0369A1"), // azzurro
        Color.FromArgb("#4338CA"), // indaco
        Color.FromArgb("#334155"), // slate
    };

    /// <summary>Colori selezionabili dall'utente: gli stessi che l'app assegnerebbe da sola.</summary>
    public static IReadOnlyList<Color> Colors { get; } = Palette;

    /// <summary>
    /// Colore del riquadro di una carta: quello scelto dall'utente se valorizzato,
    /// altrimenti quello automatico derivato dal nome. Unico punto di verità per la
    /// griglia e per la barra delle carte usate di recente.
    /// </summary>
    public static Color ForCard(Card? card)
    {
        if (card is null)
        {
            return ForName(null);
        }

        return TryParse(card.TileColor, out var chosen) ? chosen : ForName(card.DisplayName);
    }

    /// <summary>Colore automatico derivato dal nome (indice = hash stabile % N).</summary>
    public static Color ForName(string? name)
    {
        var key = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
        var index = (int)(StableHash(key) % (uint)Palette.Length);
        return Palette[index];
    }

    /// <summary>Converte un hex persistito in colore; falso se assente o non valido.</summary>
    public static bool TryParse(string? hex, out Color color)
    {
        color = Microsoft.Maui.Graphics.Colors.Transparent;
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        try
        {
            color = Color.FromArgb(hex);
            return true;
        }
        catch (Exception)
        {
            // Valore non interpretabile (db manipolato, formato inatteso): si ricade
            // sul colore automatico invece di far fallire il rendering della lista.
            return false;
        }
    }

    /// <summary>Forma canonica dell'hex da persistere in <c>Card.TileColor</c>.</summary>
    public static string ToHex(Color color) => color.ToArgbHex();

    /// <summary>
    /// Hash deterministico (FNV-1a a 32 bit). NON usare string.GetHashCode():
    /// non è stabile tra processi/run, quindi il colore cambierebbe a ogni riavvio.
    /// </summary>
    private static uint StableHash(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;

        var hash = offset;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}
