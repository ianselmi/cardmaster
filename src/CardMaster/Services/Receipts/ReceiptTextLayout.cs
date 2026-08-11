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
    /// Quanto può essere alta una riga, in multipli dell'altezza dei suoi frammenti, prima che
    /// smetta di accettarne altri.
    /// <para>
    /// Serve contro la <b>concatenazione</b>: su una foto storta il frammento A si sovrappone a
    /// B, B a C, C a D, e per transitività una sola "riga" finisce per inghiottire mezzo
    /// scontrino. Visto su scontrino reale l'11 ago 2026: sette prodotti in una riga, con tutte
    /// le loro aliquote e tutti i loro prezzi in coda. Il limite non impedisce a una riga di
    /// avere caratteri di altezza diversa — è relativo al frammento più alto che contiene — ma
    /// impedisce che cresca all'infinito.
    /// </para>
    /// </summary>
    private const double MaxRowHeightRatio = 1.7;

    /// <summary>
    /// Testo dello scontrino con le righe ricostruite: una riga per banda verticale, i
    /// frammenti ordinati da sinistra a destra e separati da spazi.
    /// </summary>
    public static string ToVisualText(OcrResult result)
    {
        var lines = ToVisualLines(result);
        return string.Join("\n", lines);
    }

    /// <summary>Righe visive come testo, dall'alto verso il basso.</summary>
    public static List<string> ToVisualLines(OcrResult result) =>
        ToVisualLayout(result).Select(l => l.Text).ToList();

    /// <summary>
    /// Righe visive <b>con la loro geometria</b>, dall'alto verso il basso.
    /// <para>
    /// È il raggruppamento per banda verticale di sempre — l'unica euristica del progetto
    /// verificata su OCR reale — esposto senza appiattirlo in stringhe.
    /// <see cref="ToVisualText"/> e <see cref="ToVisualLines"/> sono costruiti sopra questo
    /// metodo apposta: se la ricostruzione delle righe prodotto avesse una copia propria del
    /// raggruppamento, le due divergerebbero al primo aggiustamento.
    /// </para>
    /// </summary>
    public static List<ReceiptVisualLine> ToVisualLayout(OcrResult result)
    {
        var fragments = result.Blocks
            .SelectMany(b => b.Lines)
            .Where(l => !string.IsNullOrWhiteSpace(l.Text) && l.Bounds.Height > 0)
            .OrderBy(l => l.Bounds.Center.Y)
            .ToList();

        if (fragments.Count == 0)
        {
            // Nessuna geometria utilizzabile: meglio il testo grezzo che niente. Le righe che
            // ne escono non hanno colonne, e infatti non producono righe prodotto.
            return string.IsNullOrWhiteSpace(result.Text)
                ? []
                : result.Text
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ReceiptVisualLine.FromText)
                    .ToList();
        }

        // Si raggruppa sulle quote <b>raddrizzate</b>: su una foto storta la descrizione e il suo
        // prezzo, lontani ottocento pixel in orizzontale, cadono a quote diverse, e nessuna
        // tolleranza verticale sa distinguere "stessa riga inclinata" da "riga successiva".
        var slope = EstimateSlope(fragments);
        var placed = fragments
            .Select(f => new Placed(f, f.Bounds.Top - (slope * f.Bounds.Center.X)))
            .OrderBy(p => p.Top)
            .ToList();

        var rows = new List<List<Placed>>();
        foreach (var fragment in placed)
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
            .OrderBy(r => r.Min(f => f.Center))
            .Select(r => ReceiptVisualLine.FromFragments(r.Select(p => p.Fragment)))
            .Where(l => l.Text.Length > 0)
            .ToList();
    }

    /// <summary>
    /// Frammento con la sua quota <b>raddrizzata</b>: la <c>y</c> che avrebbe se lo scontrino
    /// fosse stato fotografato dritto. L'originale resta, perché è quello che finisce nella riga.
    /// </summary>
    private readonly record struct Placed(OcrLine Fragment, double Top)
    {
        public double Height => Fragment.Bounds.Height;

        public double Bottom => Top + Height;

        public double Center => Top + (Height / 2);
    }

    /// <summary>
    /// Inclinazione dello scontrino nella foto, come pendenza <c>dy/dx</c>.
    /// <para>
    /// Si stima dalle coppie di frammenti abbastanza vicine in verticale da poter stare sulla
    /// stessa riga: quelle che ci stanno davvero danno tutte la stessa pendenza, le altre danno
    /// valori sparsi, e la <b>mediana</b> tiene le prime e scarta le seconde. Su uno scontrino
    /// dritto viene zero e non cambia niente — motivo per cui questa correzione non ha bisogno
    /// di sapere se la foto è storta.
    /// </para>
    /// </summary>
    private static double EstimateSlope(List<OcrLine> fragments)
    {
        if (fragments.Count < 4)
        {
            return 0;
        }

        var slopes = new List<double>();
        for (var i = 0; i < fragments.Count; i++)
        {
            var a = fragments[i].Bounds;
            for (var j = i + 1; j < fragments.Count; j++)
            {
                var b = fragments[j].Bounds;

                var dx = b.Center.X - a.Center.X;
                if (Math.Abs(dx) < a.Height * 4)
                {
                    // Troppo vicini in orizzontale: il rapporto sarebbe dominato dal rumore.
                    continue;
                }

                var dy = b.Center.Y - a.Center.Y;
                if (Math.Abs(dy) > a.Height)
                {
                    // Troppo distanti in verticale per essere la stessa riga, comunque inclinata.
                    continue;
                }

                slopes.Add(dy / dx);
            }
        }

        if (slopes.Count < 3)
        {
            return 0;
        }

        slopes.Sort();
        var median = slopes[slopes.Count / 2];

        // Oltre i ~15° non è più una foto storta ma un'inquadratura da rifare: meglio non
        // raddrizzare che raddrizzare male.
        return Math.Abs(median) > 0.27 ? 0 : median;
    }

    /// <summary>Vero se il frammento sta nella stessa banda verticale della riga.</summary>
    private static bool IsSameRow(List<Placed> row, Placed fragment)
    {
        if (WouldGrowTooTall(row, fragment))
        {
            return false;
        }

        foreach (var existing in row)
        {
            var top = Math.Max(existing.Top, fragment.Top);
            var bottom = Math.Min(existing.Bottom, fragment.Bottom);
            var overlap = bottom - top;
            if (overlap <= 0)
            {
                continue;
            }

            var shortest = Math.Min(existing.Height, fragment.Height);
            if (shortest > 0 && overlap / shortest >= SameLineOverlapRatio)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Vero se accogliere il frammento renderebbe la riga più alta di quanto una riga di testo
    /// possa essere. È il freno alla concatenazione su scontrini fotografati storti.
    /// </summary>
    private static bool WouldGrowTooTall(List<Placed> row, Placed fragment)
    {
        var top = Math.Min(row.Min(f => f.Top), fragment.Top);
        var bottom = Math.Max(row.Max(f => f.Bottom), fragment.Bottom);
        var tallest = Math.Max(row.Max(f => f.Height), fragment.Height);

        return tallest > 0 && (bottom - top) > tallest * MaxRowHeightRatio;
    }
}
