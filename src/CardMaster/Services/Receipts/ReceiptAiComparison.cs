namespace CardMaster.Services.Receipts;

/// <summary>Quale lettura proporre all'utente dopo una rilettura.</summary>
public enum ReceiptReadingChoice
{
    /// <summary>Si tengono le righe locali: la rilettura non ha migliorato niente.</summary>
    KeepLocal = 0,

    /// <summary>Si propongono le righe del modello: quadrano dove le locali non quadravano.</summary>
    UseAi = 1,

    /// <summary>Non quadra nessuna delle due: va detto, e la correzione resta all'utente.</summary>
    NeitherBalances = 2,
}

/// <summary>
/// Esito del confronto.
/// </summary>
/// <param name="Choice">Cosa proporre.</param>
/// <param name="AiIsCloser">
/// Quando non quadra nessuno dei due, se la lettura del modello è comunque più vicina al totale.
/// Serve a mostrare la migliore delle due <b>dicendo</b> che nemmeno quella quadra, invece di
/// scegliere in silenzio.
/// </param>
public readonly record struct ReceiptReadingComparison(ReceiptReadingChoice Choice, bool AiIsCloser);

/// <summary>
/// Decide se le righe rilette dal modello vadano proposte al posto di quelle locali.
/// <para>
/// La regola è una sola e misurabile: <b>si sostituisce solo ciò che non quadrava</b>. Righe che
/// quadrano non vengono mai rimpiazzate in silenzio dall'esito di un modello — l'esito passa per
/// la stessa <see cref="ReceiptTotalsCheck"/> delle altre e deve guadagnarsi il posto.
/// </para>
/// </summary>
public static class ReceiptAiComparison
{
    public static ReceiptReadingComparison Compare(ReceiptBalance local, ReceiptBalance ai)
    {
        var localBalanced = local.Status == ReceiptBalanceStatus.Balanced;
        var aiBalanced = ai.Status == ReceiptBalanceStatus.Balanced;

        if (aiBalanced && !localBalanced)
        {
            return new ReceiptReadingComparison(ReceiptReadingChoice.UseAi, AiIsCloser: true);
        }

        // Le locali quadrano: non si tocca niente, quale che sia l'esito del modello. Vale anche
        // quando quadrano entrambe — sostituire righe corrette con altre righe corrette è
        // movimento senza guadagno, e toglie all'utente il lavoro di correzione già fatto.
        if (localBalanced)
        {
            return new ReceiptReadingComparison(ReceiptReadingChoice.KeepLocal, AiIsCloser: false);
        }

        // Nessuna delle due quadra. Non si sceglie in silenzio: si dice, e si indica quale delle
        // due è più vicina al totale stampato — che è un'informazione, non una promessa.
        var localGap = Math.Abs(local.DifferenceCents);
        var aiGap = Math.Abs(ai.DifferenceCents);

        // A parità di scarto non si scomodano le righe locali: il pari non è un miglioramento.
        var aiIsCloser = ai.Status != ReceiptBalanceStatus.NotChecked && aiGap < localGap;

        return new ReceiptReadingComparison(ReceiptReadingChoice.NeitherBalances, aiIsCloser);
    }
}
