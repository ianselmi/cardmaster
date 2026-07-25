using Microsoft.Maui.Graphics;

namespace CardMaster.Views;

/// <summary>
/// Palette curata per i riquadri della lista carte. Colori mid/saturi pensati per
/// essere leggibili con testo bianco. Il colore di una carta è scelto in modo
/// deterministico dal suo nome (stessa carta → stesso colore).
/// </summary>
public static class CardTilePalette
{
    // Palette allineata al brand ambra/arancio (change maui-restyle). Toni scuri/saturi
    // per leggere bene con testo bianco. Nessun tile coincide con l'ambra dei controlli
    // (Primary #E07B1A), per non confondere colore-azione e colore-carta.
    private static readonly Color[] Colors =
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

    /// <summary>Colore del tile derivato dal nome (indice = hash stabile % N).</summary>
    public static Color ForName(string? name)
    {
        var key = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
        var index = (int)(StableHash(key) % (uint)Colors.Length);
        return Colors[index];
    }

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
