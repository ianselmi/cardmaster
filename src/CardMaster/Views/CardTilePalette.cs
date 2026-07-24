using Microsoft.Maui.Graphics;

namespace CardMaster.Views;

/// <summary>
/// Palette curata per i riquadri della lista carte. Colori mid/saturi pensati per
/// essere leggibili con testo bianco. Il colore di una carta è scelto in modo
/// deterministico dal suo nome (stessa carta → stesso colore).
/// </summary>
public static class CardTilePalette
{
    // Colori sufficientemente scuri/saturi da leggere bene con testo bianco.
    private static readonly Color[] Colors =
    {
        Color.FromArgb("#E2001A"), // rosso
        Color.FromArgb("#004996"), // blu
        Color.FromArgb("#0082C3"), // azzurro
        Color.FromArgb("#2E7D32"), // verde
        Color.FromArgb("#6A1B9A"), // viola
        Color.FromArgb("#EF6C00"), // arancio
        Color.FromArgb("#00838F"), // teal
        Color.FromArgb("#AD1457"), // magenta
        Color.FromArgb("#4527A0"), // indaco
        Color.FromArgb("#37474F"), // blu-grigio
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
