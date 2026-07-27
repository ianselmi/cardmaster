using System.Text;
using CardMaster.Services;

namespace CardMaster.Data;

/// <summary>
/// Regole delle label di una carta: serializzazione nella colonna <c>LabelsCsv</c>,
/// normalizzazione del testo e deduplicazione. Le label vivono sulla riga della carta
/// (niente tabelle separate: una relazione molti-a-molti con tombstone propri sarebbe il
/// caso peggiore per il last-write-wins per riga previsto in v2).
/// </summary>
public static class CardLabels
{
    /// <summary>Separatore interno: vietato nel testo di una label (viene rimosso dalla normalizzazione).</summary>
    public const char Separator = '|';

    /// <summary>Lunghezza massima di una singola label (oltre, il testo viene troncato).</summary>
    public const int MaxLength = 24;

    /// <summary>Numero massimo di label per carta.</summary>
    public const int MaxPerCard = 8;

    /// <summary>Label serializzate → lista tipizzata. Tollera valori vuoti o malformati.</summary>
    public static List<string> Parse(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return new List<string>();
        }

        return serialized
            .Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <summary>Lista tipizzata → valore della colonna. Lista vuota → <c>null</c> (colonna non valorizzata).</summary>
    public static string? Serialize(IEnumerable<string>? labels)
    {
        if (labels is null)
        {
            return null;
        }

        var joined = string.Join(Separator, labels.Where(l => !string.IsNullOrWhiteSpace(l)));
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    /// <summary>
    /// Testo digitato → label assegnabile: trim, spazi interni collassati, caratteri di
    /// controllo e separatore rimossi, troncamento a <see cref="MaxLength"/>.
    /// Restituisce stringa vuota se non resta nulla di assegnabile.
    /// </summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(raw.Length);
        var lastWasSpace = false;

        foreach (var c in raw)
        {
            if (c == Separator || char.IsControl(c))
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                // Collassa gli spazi interni; quelli iniziali non entrano mai (builder vuoto).
                if (builder.Length > 0)
                {
                    lastWasSpace = true;
                }

                continue;
            }

            if (lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = false;
            }

            builder.Append(c);
        }

        var normalized = builder.ToString();
        return normalized.Length > MaxLength ? normalized[..MaxLength].TrimEnd() : normalized;
    }

    /// <summary>
    /// Vero se le due label sono la stessa label (confronto case/accent-insensitive:
    /// "Spesa", "spesa" e "SPESA" sono una sola label, come "citta" trova "Città").
    /// </summary>
    public static bool AreSame(string? a, string? b)
        => TextNormalizer.Normalize(a) == TextNormalizer.Normalize(b);

    /// <summary>
    /// Aggiunge una label a una lista esistente applicando normalizzazione, deduplicazione
    /// (conserva la grafia già presente) e il limite di <see cref="MaxPerCard"/>.
    /// </summary>
    /// <returns>L'esito, per distinguere il limite raggiunto da un duplicato ignorato.</returns>
    public static AddLabelResult TryAdd(IList<string> labels, string? raw)
    {
        var normalized = Normalize(raw);
        if (normalized.Length == 0)
        {
            return AddLabelResult.Empty;
        }

        if (labels.Any(l => AreSame(l, normalized)))
        {
            return AddLabelResult.Duplicate;
        }

        if (labels.Count >= MaxPerCard)
        {
            return AddLabelResult.LimitReached;
        }

        labels.Add(normalized);
        return AddLabelResult.Added;
    }

    /// <summary>
    /// Normalizza e deduplica un insieme di label già esistenti (es. caricate dal database
    /// o ricevute da un'altra sorgente), rispettando il limite per carta.
    /// </summary>
    public static List<string> Sanitize(IEnumerable<string>? labels)
    {
        var result = new List<string>();
        if (labels is null)
        {
            return result;
        }

        foreach (var label in labels)
        {
            TryAdd(result, label);
        }

        return result;
    }
}

/// <summary>Esito di <see cref="CardLabels.TryAdd"/>.</summary>
public enum AddLabelResult
{
    /// <summary>Label aggiunta.</summary>
    Added,

    /// <summary>Testo vuoto dopo la normalizzazione: nessuna label da aggiungere.</summary>
    Empty,

    /// <summary>La carta ha già questa label (confronto case/accent-insensitive).</summary>
    Duplicate,

    /// <summary>Raggiunto il numero massimo di label per carta.</summary>
    LimitReached,
}
