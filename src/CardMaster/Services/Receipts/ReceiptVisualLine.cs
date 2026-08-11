namespace CardMaster.Services.Receipts;

/// <summary>
/// Riga visiva dello scontrino: i frammenti che l'OCR ha riconosciuto sulla stessa banda
/// verticale, <b>con la loro geometria</b>.
/// <para>
/// Esiste perché appiattire la riga in una stringa butta via l'unica informazione che dice
/// quali sono le colonne: la distanza orizzontale tra descrizione e prezzo. Per la testata
/// bastava il testo (<c>TOTALE COMPLESSIVO  6,61</c> è leggibile da una regex); per le righe
/// prodotto no, perché <c>PROSCIUTTO 100 GR  4,50</c> e una descrizione che contiene un numero
/// si distinguono solo guardando dove cade il numero.
/// </para>
/// </summary>
/// <param name="Text">
/// Testo della riga, frammenti da sinistra a destra separati da due spazi. È esattamente ciò
/// che il layout produceva prima di esporre la geometria: la testata continua a leggere questo.
/// </param>
/// <param name="Fragments">
/// Frammenti ordinati da sinistra a destra. Vuoto quando la riga arriva dal testo grezzo,
/// cioè quando l'OCR non ha restituito geometria utilizzabile.
/// </param>
/// <param name="Bounds">Rettangolo che racchiude tutti i frammenti della riga.</param>
public readonly record struct ReceiptVisualLine(
    string Text,
    IReadOnlyList<OcrLine> Fragments,
    Rect Bounds)
{
    /// <summary>Vero se la riga porta con sé la geometria, e non solo il testo.</summary>
    public bool HasGeometry => Fragments.Count > 0;

    /// <summary>
    /// Riga senza geometria, ricavata dal testo grezzo. Le righe prodotto non sono
    /// ricostruibili da una riga così, ed è giusto che si veda dal tipo.
    /// </summary>
    public static ReceiptVisualLine FromText(string text) => new(text, [], default);

    /// <summary>Riga costruita dai frammenti di una banda verticale.</summary>
    public static ReceiptVisualLine FromFragments(IEnumerable<OcrLine> fragments)
    {
        var ordered = fragments.OrderBy(f => f.Bounds.Left).ToList();
        if (ordered.Count == 0)
        {
            return FromText(string.Empty);
        }

        var text = string.Join("  ", ordered.Select(f => f.Text.Trim())).Trim();

        var left = ordered.Min(f => f.Bounds.Left);
        var top = ordered.Min(f => f.Bounds.Top);
        var right = ordered.Max(f => f.Bounds.Right);
        var bottom = ordered.Max(f => f.Bounds.Bottom);

        return new ReceiptVisualLine(text, ordered, new Rect(left, top, right - left, bottom - top));
    }
}
