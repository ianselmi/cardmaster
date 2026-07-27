using System.Globalization;
using System.Text;

namespace CardMaster.Services;

/// <summary>
/// Normalizzazione del testo per confronti case-insensitive e accent-insensitive
/// (es. "citta" trova "Città"). Usata dalla ricerca nella lista carte e dal confronto
/// tra label: la stessa regola in un solo punto, così "Spesa" e "spesa" restano una
/// sola label esattamente come "citta" trova "Città".
/// </summary>
public static class TextNormalizer
{
    /// <summary>Forma normalizzata per il confronto: minuscola, senza segni diacritici.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
