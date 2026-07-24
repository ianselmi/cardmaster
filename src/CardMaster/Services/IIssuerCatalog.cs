using CardMaster.Data;

namespace CardMaster.Services;

/// <summary>
/// Accesso al catalogo emittenti (seed statico bundle nell'app). Read-only, offline,
/// senza sync. Fornisce suggerimenti: l'associazione di una carta a un emittente è
/// facoltativa, quindi i metodi di ricerca restituiscono null senza errori quando non
/// c'è corrispondenza.
/// </summary>
public interface IIssuerCatalog
{
    /// <summary>Elenco completo degli emittenti del catalogo.</summary>
    Task<IReadOnlyList<Issuer>> GetAllAsync();

    /// <summary>Emittente per id, oppure null se non presente.</summary>
    Task<Issuer?> GetByIdAsync(string id);

    /// <summary>
    /// Emittente il cui nome o alias corrisponde al testo (case-insensitive),
    /// oppure null se nessuna corrispondenza.
    /// </summary>
    Task<Issuer?> MatchAsync(string text);
}
