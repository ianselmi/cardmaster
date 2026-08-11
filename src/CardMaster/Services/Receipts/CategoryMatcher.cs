using CardMaster.Data;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Classifica la descrizione di una riga in una categoria di spesa.
/// <para>
/// Riceve il dizionario <b>come dato</b> e non lo carica: il caricamento dal bundle sta in
/// <see cref="ICategoryCatalog"/>. Così questa resta una classe pura, verificabile senza
/// emulatore — che è la sola ragione per cui le sue regole si possono cambiare senza paura.
/// </para>
/// <para>
/// Il confronto è per <b>token contenuti e prefisso</b>, non per distanza di edit. Una distanza
/// libera su descrizioni di otto caratteri accoppia <c>MELE</c> e <c>MIELE</c>, che sono due
/// categorie diverse: un falso positivo silenzioso in classificazione è peggio di una riga non
/// classificata, che almeno si vede.
/// </para>
/// </summary>
public static class CategoryMatcher
{
    /// <summary>
    /// Lunghezza minima di un token perché valga come prefisso di una parola chiave.
    /// <c>past</c> deve trovare <c>pasta</c>; <c>pan</c> non deve scegliere tra <c>pane</c> e
    /// <c>pannolini</c>.
    /// </summary>
    private const int MinimumPrefixLength = 4;

    /// <summary>Caratteri che separano i token in una descrizione da scontrino.</summary>
    private static readonly char[] Separators =
        [' ', '.', ',', '/', '-', '_', '(', ')', '*', '+', '\'', '"', ':', ';', '\t'];

    /// <summary>
    /// Categoria della descrizione secondo il solo dizionario, <c>null</c> se nessuna
    /// corrispondenza. Nessuna categoria di ripiego: "non classificata" è un esito, non un errore.
    /// </summary>
    public static string? Match(string? description, IReadOnlyList<ProductCategory> categories)
    {
        if (string.IsNullOrWhiteSpace(description) || categories.Count == 0)
        {
            return null;
        }

        var normalized = TextNormalizer.Normalize(description);
        var tokens = normalized.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return null;
        }

        string? bestCategory = null;
        var bestLength = 0;

        foreach (var category in categories)
        {
            foreach (var keyword in category.Keywords)
            {
                var normalizedKeyword = TextNormalizer.Normalize(keyword);
                if (normalizedKeyword.Length == 0 || normalizedKeyword.Length <= bestLength)
                {
                    continue;
                }

                if (Matches(normalized, tokens, normalizedKeyword))
                {
                    bestCategory = category.Id;
                    bestLength = normalizedKeyword.Length;
                }
            }
        }

        return bestCategory;
    }

    /// <summary>
    /// Categoria della riga, mappature apprese <b>prima</b> del dizionario: è l'utente ad aver
    /// visto quel prodotto, e la sua correzione vale più di una parola chiave generica.
    /// </summary>
    public static string? Resolve(
        string? description,
        IReadOnlyDictionary<string, string> learned,
        IReadOnlyList<ProductCategory> categories)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var key = TextNormalizer.Normalize(description);
        return learned.TryGetValue(key, out var category) && !string.IsNullOrWhiteSpace(category)
            ? category
            : Match(description, categories);
    }

    /// <summary>
    /// Corrispondenza di una parola chiave: come frase dentro la descrizione, come token uguale,
    /// oppure come parola di cui un token della descrizione è prefisso — che è il caso delle
    /// abbreviazioni puntate dello scontrino (<c>PAST.BARILLA</c> → <c>pasta</c>).
    /// </summary>
    private static bool Matches(string normalized, string[] tokens, string keyword)
    {
        if (keyword.Contains(' ', StringComparison.Ordinal))
        {
            return normalized.Contains(keyword, StringComparison.Ordinal);
        }

        foreach (var token in tokens)
        {
            if (string.Equals(token, keyword, StringComparison.Ordinal))
            {
                return true;
            }

            if (token.Length >= MinimumPrefixLength &&
                token.Length < keyword.Length &&
                keyword.StartsWith(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
