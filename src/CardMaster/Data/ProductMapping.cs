using SQLite;

namespace CardMaster.Data;

/// <summary>Da dove viene una mappatura prodotto → categoria.</summary>
public enum ProductMappingOrigin
{
    /// <summary>Dal dizionario seed, confermata o corretta dall'utente.</summary>
    Seed = 0,

    /// <summary>Scritta dall'utente correggendo la categoria di una riga.</summary>
    User = 1,

    /// <summary>
    /// Prodotta da un modello linguistico. Non usata in questa change: esiste perché la regola
    /// "una mappatura dell'utente non viene mai sovrascritta da una automatica" deve poter
    /// essere applicata senza ambiguità quando quella change arriverà.
    /// </summary>
    Ai = 2,
}

/// <summary>
/// Mappatura appresa da una correzione dell'utente: questa descrizione, questa categoria.
/// <para>
/// La spesa è ripetitiva: dopo qualche settimana il grosso del carrello abituale è già
/// classificato senza che l'utente debba fare altro. Vale <b>dagli scontrini successivi</b> —
/// riscrivere all'indietro dati storici è una decisione dell'utente, non un effetto collaterale.
/// </para>
/// <para>
/// La tabella nasce già con <see cref="DisplayName"/> e <see cref="Origin"/>, che oggi non
/// servono: <c>receipt-ai-normalize</c> è dichiarata cache-first su questa tabella, e nascere
/// con due colonne in più costa zero mentre migrarla dopo costa una migrazione su dati veri.
/// </para>
/// </summary>
public class ProductMapping : EntityBase
{
    /// <summary>
    /// Descrizione normalizzata con <c>TextNormalizer</c>: la stessa regola di ricerca e label,
    /// non una seconda.
    /// </summary>
    [Indexed(Unique = true)]
    public string NormalizedDescription { get; set; } = string.Empty;

    /// <summary>Categoria assegnata.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Nome leggibile del prodotto. Vuoto in questa change.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Origine della mappatura.</summary>
    public ProductMappingOrigin Origin { get; set; } = ProductMappingOrigin.User;
}
