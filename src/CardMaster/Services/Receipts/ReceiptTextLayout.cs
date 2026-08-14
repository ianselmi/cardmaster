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
    /// Quanti frammenti servono perché una fascia meriti una stima di pendenza propria. Sotto
    /// questa soglia le coppie disponibili sono poche e la mediana diventa rumore: meglio la
    /// pendenza dell'intero scontrino, che è meno precisa ma non inventa.
    /// </summary>
    private const int MinFragmentsPerBand = 24;

    /// <summary>
    /// Quante fasce al massimo. Poche fasce alte descrivono male la curvatura, molte fasce basse
    /// non hanno abbastanza frammenti per stimarla: cinque è il compromesso su uno scontrino
    /// della spesa, dove una fascia copre una manciata di righe.
    /// </summary>
    private const int MaxBands = 5;

    /// <summary>
    /// Quanto può valere, al massimo, la correzione di una singola fascia oltre la pendenza
    /// generale. Una curvatura vera cambia gradualmente lungo la carta; un salto più brusco è
    /// quasi sempre una fascia con poche coppie che ha stimato male, e va limitato.
    /// </summary>
    private const double MaxResidualSlope = 0.12;

    /// <summary>
    /// Quante volte si ripete la correzione per fascia. Ogni passata recupera le righe che la
    /// precedente non riusciva a misurare, con rendimenti decrescenti: quattro coprono la
    /// curvatura di uno scontrino appoggiato storto, e il ciclo esce prima appena non trova più
    /// niente da raddrizzare.
    /// </summary>
    private const int BandPasses = 4;

    /// <summary>
    /// Pendenza sotto la quale non vale la pena di un'altra passata: su una riga larga quanto uno
    /// scontrino sposta meno di un pixel.
    /// </summary>
    private const double NegligibleSlope = 0.002;

    /// <summary>
    /// Quanto due frammenti possono distare in verticale, in multipli della loro altezza, per
    /// essere presi come coppia da cui stimare la pendenza: più in là non sono la stessa riga.
    /// <para>
    /// Vale identico per la stima generale e per quella di fascia. Allentarlo sul residuo
    /// sembrava sensato — i frammenti sono già raddrizzati, e con il limite stretto le righe più
    /// deformate restano fuori — ed è invece <b>controproducente</b>: su una riga inclinata verso
    /// il basso, l'importo della riga <em>successiva</em> risale fin dentro la tolleranza, entra
    /// come coppia con il segno opposto e trascina la mediana verso lo zero. Misurato: le fasce
    /// stimavano <c>-0,012</c> e <c>+0,006</c> dove la pendenza vera era <c>-0,045</c> e
    /// <c>+0,045</c>. Meglio poche coppie giuste che molte coppie contraddittorie.
    /// </para>
    /// </summary>
    private const double SameRowDistanceRatio = 1.0;

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
        //
        // Il raddrizzamento è in due passaggi. Il primo usa una pendenza sola per tutto lo
        // scontrino, e basta finché la carta è piana. Il secondo corregge fascia per fascia quel
        // che resta, perché uno scontrino appoggiato è quasi sempre anche <b>incurvato</b>: la
        // pendenza del centro non è quella delle estremità, e con la sola mediana generale gli
        // importi di cima e fondo scivolano di mezza riga finendo appaiati al prodotto vicino.

        // La pendenza generale si stima una volta sola. Ripeterla come si fa più sotto con le
        // fasce sembra la stessa idea e non lo è: su uno scontrino incurvato la mediana generale
        // converge verso l'inclinazione di una metà, e raddrizzando anche l'altra la peggiora.
        // Misurato, e infatti il caso incurvato tornava a spezzarsi.
        var raw = fragments.Select(f => new Placed(f, f.Bounds.Top)).ToList();
        var slope = EstimateSlope(raw, SameRowDistanceRatio) ?? 0;
        var placed = raw.Select(p => p.Straighten(slope)).OrderBy(p => p.Top).ToList();

        // Il secondo passaggio si ripete, perché una passata sola sottostima. Le coppie da cui si
        // misura la pendenza sono quelle abbastanza vicine in verticale da poter essere la stessa
        // riga, e le righe più deformate — quelle da correggere — cadono fuori da quel limite: la
        // fascia le esclude e stima sul resto, cioè meno del vero. Ma ogni passata raddrizza un
        // po', quelle righe rientrano nel limite, e la passata dopo le vede. Su uno scontrino
        // piano la prima stima è già zero e il ciclo esce subito.
        for (var pass = 0; pass < BandPasses; pass++)
        {
            var bands = EstimateBandResiduals(placed);
            if (bands.Count == 0 || bands.TrueForAll(b => Math.Abs(b.Slope) < NegligibleSlope))
            {
                break;
            }

            placed = placed
                .Select(p => p.Straighten(ResidualAt(bands, p.Center)))
                .OrderBy(p => p.Top)
                .ToList();
        }

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

        public double X => Fragment.Bounds.Center.X;

        /// <summary>Rialza il frammento della pendenza indicata: applicabile più volte, perché
        /// le pendenze si sommano e ogni passaggio corregge quel che il precedente ha lasciato.
        /// </summary>
        public Placed Straighten(double slope) => this with { Top = Top - (slope * X) };
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
    /// <param name="fragments">Frammenti alle loro quote correnti, già raddrizzate o no.</param>
    /// <param name="distanceRatio">
    /// Quanto due frammenti possono distare in verticale per essere presi come coppia, in
    /// multipli della loro altezza.
    /// </param>
    /// <returns>La pendenza, oppure <c>null</c> se i dati non bastano a stimarla.</returns>
    private static double? EstimateSlope(List<Placed> fragments, double distanceRatio)
    {
        if (fragments.Count < 4)
        {
            return null;
        }

        var slopes = new List<double>();
        for (var i = 0; i < fragments.Count; i++)
        {
            var a = fragments[i];
            for (var j = i + 1; j < fragments.Count; j++)
            {
                var b = fragments[j];

                var dx = b.X - a.X;
                if (Math.Abs(dx) < a.Height * 4)
                {
                    // Troppo vicini in orizzontale: il rapporto sarebbe dominato dal rumore.
                    continue;
                }

                var dy = b.Center - a.Center;
                if (Math.Abs(dy) > a.Height * distanceRatio)
                {
                    // Troppo distanti in verticale per essere la stessa riga, comunque inclinata.
                    continue;
                }

                slopes.Add(dy / dx);
            }
        }

        if (slopes.Count < 3)
        {
            return null;
        }

        slopes.Sort();
        var median = slopes[slopes.Count / 2];

        // Oltre i ~15° non è più una foto storta ma un'inquadratura da rifare: meglio non
        // raddrizzare che raddrizzare male.
        return Math.Abs(median) > 0.27 ? null : median;
    }

    /// <summary>
    /// Pendenza <b>residua</b> fascia per fascia: quanto la carta è ancora inclinata a ciascuna
    /// quota, dopo che il raddrizzamento generale ha fatto la sua parte.
    /// <para>
    /// Le fasce si tagliano a pari numero di frammenti e non a pari altezza: la testata e il
    /// riepilogo IVA sono molto più radi del corpo, e fasce di uguale altezza lascerebbero le
    /// prime senza coppie a sufficienza mentre il corpo ne sprecherebbe.
    /// </para>
    /// </summary>
    /// <param name="straightened">Frammenti già raddrizzati, ordinati per quota.</param>
    /// <returns>
    /// Quota centrale e pendenza residua di ogni fascia stimabile, dall'alto verso il basso.
    /// Lista vuota quando lo scontrino è troppo corto per dire alcunché sulla curvatura: in quel
    /// caso vale la sola pendenza generale, cioè il comportamento di prima.
    /// </returns>
    private static List<(double Center, double Slope)> EstimateBandResiduals(List<Placed> straightened)
    {
        var bandCount = Math.Min(straightened.Count / MinFragmentsPerBand, MaxBands);
        if (bandCount < 2)
        {
            return [];
        }

        var bands = new List<(double Center, double Slope)>(bandCount);
        for (var i = 0; i < bandCount; i++)
        {
            var from = i * straightened.Count / bandCount;
            var to = (i + 1) * straightened.Count / bandCount;
            var band = straightened.GetRange(from, to - from);

            var slope = EstimateSlope(band, SameRowDistanceRatio);
            if (slope is null)
            {
                // Fascia che non sa dire la propria pendenza: si lascia fuori invece di
                // dichiararla piana, così l'interpolazione la copre con le vicine.
                continue;
            }

            var center = band[band.Count / 2].Center;
            bands.Add((center, Math.Clamp(slope.Value, -MaxResidualSlope, MaxResidualSlope)));
        }

        return bands.Count < 2 ? [] : bands;
    }

    /// <summary>
    /// Pendenza residua alla quota indicata, <b>interpolata</b> tra le fasce vicine.
    /// <para>
    /// L'interpolazione non è un raffinamento ma la condizione perché il metodo funzioni: con una
    /// pendenza costante per fascia, due frammenti della stessa riga capitati a cavallo di un
    /// confine riceverebbero correzioni diverse, e la riga si spezzerebbe esattamente dove il
    /// rimedio doveva ricomporla.
    /// </para>
    /// <para>
    /// Ogni fascia parla per la propria <b>quota centrale</b>, quindi sopra la prima e sotto
    /// l'ultima la pendenza va <b>estrapolata</b>, non tenuta ferma: le prime e le ultime righe
    /// sono le più deformate — la curvatura si accumula proprio agli estremi — e fermare la
    /// correzione al centro delle fasce di bordo lascerebbe scoperta metà del difetto. Il
    /// prolungamento è breve, mezza fascia, e resta comunque entro
    /// <see cref="MaxResidualSlope"/>.
    /// </para>
    /// </summary>
    private static double ResidualAt(List<(double Center, double Slope)> bands, double y)
    {
        var upper = bands.FindIndex(b => y <= b.Center);
        var segment = upper switch
        {
            0 => 1,                 // sopra la prima fascia: si prolunga il primo tratto
            -1 => bands.Count - 1,  // sotto l'ultima: si prolunga l'ultimo
            _ => upper,
        };

        var (fromCenter, fromSlope) = bands[segment - 1];
        var (toCenter, toSlope) = bands[segment];

        var span = toCenter - fromCenter;
        if (span <= 0)
        {
            return toSlope;
        }

        var residual = fromSlope + ((toSlope - fromSlope) * ((y - fromCenter) / span));
        return Math.Clamp(residual, -MaxResidualSlope, MaxResidualSlope);
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
