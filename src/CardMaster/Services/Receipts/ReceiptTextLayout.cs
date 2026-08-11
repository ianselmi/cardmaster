using System.Text;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Ricostruisce le <b>righe visive</b> dello scontrino dalla geometria dell'OCR.
/// <para>
/// Serve perché ML Kit non restituisce lo scontrino riga per riga: raggruppa il testo in
/// blocchi, e su uno scontrino a colonne questo significa <b>prima tutte le descrizioni e
/// poi tutti i prezzi</b>. Verificato su emulatore l'11 ago 2026: nel testo grezzo
/// <c>TOTALE COMPLESSIVO</c> e <c>6,61</c> finiscono a quindici righe di distanza, e nessuna
/// regola basata sull'ordine del testo può riaccoppiarli.
/// </para>
/// <para>
/// Rimettendo insieme le righe per banda verticale, <c>TOTALE COMPLESSIVO   6,61</c> torna a
/// essere una riga sola. È la stessa operazione che servirà alle righe prodotto: nasce qui
/// perché senza di essa non è estraibile nemmeno la testata.
/// </para>
/// </summary>
public static class ReceiptTextLayout
{
    /// <summary>
    /// Quanto due frammenti devono sovrapporsi in verticale per essere considerati la stessa
    /// riga, in frazione dell'altezza del frammento più basso. Sotto il 50% si rischia di
    /// fondere righe adiacenti; sopra, di spezzare una riga con caratteri di altezza diversa.
    /// </summary>
    private const double SameLineOverlapRatio = 0.5;

    /// <summary>
    /// Testo dello scontrino con le righe ricostruite: una riga per banda verticale, i
    /// frammenti ordinati da sinistra a destra e separati da spazi.
    /// </summary>
    public static string ToVisualText(OcrResult result)
    {
        var lines = ToVisualLines(result);
        return string.Join("\n", lines);
    }

    /// <summary>Righe visive, dall'alto verso il basso.</summary>
    public static List<string> ToVisualLines(OcrResult result)
    {
        var fragments = result.Blocks
            .SelectMany(b => b.Lines)
            .Where(l => !string.IsNullOrWhiteSpace(l.Text) && l.Bounds.Height > 0)
            .OrderBy(l => l.Bounds.Center.Y)
            .ToList();

        if (fragments.Count == 0)
        {
            // Nessuna geometria utilizzabile: meglio il testo grezzo che niente.
            return string.IsNullOrWhiteSpace(result.Text)
                ? []
                : result.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        var rows = new List<List<OcrLine>>();
        foreach (var fragment in fragments)
        {
            var row = rows.FirstOrDefault(r => IsSameRow(r, fragment));
            if (row is null)
            {
                rows.Add([fragment]);
            }
            else
            {
                row.Add(fragment);
            }
        }

        return rows
            .OrderBy(r => r.Min(f => f.Bounds.Center.Y))
            .Select(BuildLine)
            .Where(l => l.Length > 0)
            .ToList();
    }

    /// <summary>Vero se il frammento sta nella stessa banda verticale della riga.</summary>
    private static bool IsSameRow(List<OcrLine> row, OcrLine fragment)
    {
        foreach (var existing in row)
        {
            var top = Math.Max(existing.Bounds.Top, fragment.Bounds.Top);
            var bottom = Math.Min(existing.Bounds.Bottom, fragment.Bounds.Bottom);
            var overlap = bottom - top;
            if (overlap <= 0)
            {
                continue;
            }

            var shortest = Math.Min(existing.Bounds.Height, fragment.Bounds.Height);
            if (shortest > 0 && overlap / shortest >= SameLineOverlapRatio)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Concatena i frammenti di una riga da sinistra a destra.</summary>
    private static string BuildLine(List<OcrLine> row)
    {
        var builder = new StringBuilder();
        foreach (var fragment in row.OrderBy(f => f.Bounds.Left))
        {
            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append(fragment.Text.Trim());
        }

        return builder.ToString().Trim();
    }
}
