using System.Globalization;
using System.Text.RegularExpressions;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Lettura degli importi in formato italiano, in <b>centesimi interi</b>.
/// <para>
/// Sta in un punto solo perché testata e righe prodotto devono leggere gli importi con la
/// stessa regola: due copie divergerebbero al primo scontrino che costringe ad allargare il
/// pattern, e la differenza si vedrebbe solo come una quadratura che non torna.
/// </para>
/// </summary>
public static class ReceiptAmount
{
    /// <summary>
    /// Importo in formato italiano: virgola decimale obbligatoria, punto opzionale come
    /// separatore delle migliaia. Es. <c>1.234,56</c>, <c>7,90</c>. Lo spazio dopo la virgola
    /// è tollerato perché l'OCR spezza i gruppi di cifre.
    /// </summary>
    public static readonly Regex Pattern = new(
        @"(?<!\d)(\d{1,3}(?:\.\d{3})+|\d+),\s?(\d{2})(?!\d)",
        RegexOptions.Compiled);

    /// <summary>Segni di meno che l'OCR restituisce, incluso quello tipografico.</summary>
    private const string MinusSigns = "-−–—";

    /// <summary>Ultimo importo della riga (il prezzo sta a destra), in centesimi.</summary>
    public static long? LastCents(string line)
    {
        var matches = Pattern.Matches(line);
        return matches.Count == 0 ? null : ToCents(matches[^1]);
    }

    /// <summary>Primo importo della riga, in centesimi.</summary>
    public static long? FirstCents(string line)
    {
        var match = Pattern.Match(line);
        return match.Success ? ToCents(match) : null;
    }

    /// <summary>
    /// Vero se il frammento è <b>soltanto</b> un importo, a parte il segno e la lettera di
    /// reparto che alcuni scontrini appiccicano alla cifra (<c>4,50 A</c>). È il test che
    /// decide se un frammento può essere il prezzo di una riga: <c>PASTA 500</c> non lo è.
    /// </summary>
    public static bool IsAmountOnly(string? text, out long cents)
    {
        cents = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        var negative = false;

        if (MinusSigns.Contains(trimmed[0], StringComparison.Ordinal))
        {
            negative = true;
            trimmed = trimmed[1..].TrimStart();
        }
        else if (MinusSigns.Contains(trimmed[^1], StringComparison.Ordinal))
        {
            // Alcuni registratori stampano il segno dopo la cifra: 1,00-
            negative = true;
            trimmed = trimmed[..^1].TrimEnd();
        }

        // Valuta e lettera di reparto attaccate all'importo non lo rendono un'altra cosa.
        trimmed = trimmed.TrimEnd('€', '*', ' ');
        if (trimmed.Length > 2 && char.IsLetter(trimmed[^1]) && trimmed[^2] == ' ')
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        trimmed = trimmed.TrimStart('€', ' ');

        var match = Pattern.Match(trimmed);
        if (!match.Success || match.Index != 0 || match.Length != trimmed.Length)
        {
            return false;
        }

        var value = ToCents(match);
        if (value is null)
        {
            return false;
        }

        cents = negative ? -value.Value : value.Value;
        return true;
    }

    private static long? ToCents(Match match)
    {
        var whole = match.Groups[1].Value.Replace(".", string.Empty, StringComparison.Ordinal);
        var fraction = match.Groups[2].Value;

        if (!long.TryParse(whole, NumberStyles.None, CultureInfo.InvariantCulture, out var units) ||
            !long.TryParse(fraction, NumberStyles.None, CultureInfo.InvariantCulture, out var decimals))
        {
            return null;
        }

        return (units * 100) + decimals;
    }
}
