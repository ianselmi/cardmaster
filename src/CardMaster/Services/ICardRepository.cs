using CardMaster.Data;

namespace CardMaster.Services;

/// <summary>
/// Accesso ai dati delle carte. Le query restituiscono solo le carte attive
/// (i tombstone sono esclusi); la cancellazione è sempre logica.
/// </summary>
public interface ICardRepository
{
    /// <summary>Carte attive (tombstone esclusi), più recenti prima.</summary>
    Task<List<Card>> GetAllAsync();

    /// <summary>Carta attiva per Id, oppure null se assente o cancellata.</summary>
    Task<Card?> GetByIdAsync(string id);

    /// <summary>Inserisce una nuova carta (Id client-generato se non impostato).</summary>
    Task AddAsync(Card card);

    /// <summary>Aggiorna una carta esistente.</summary>
    Task UpdateAsync(Card card);

    /// <summary>Cancellazione LOGICA (tombstone): la riga non viene rimossa fisicamente.</summary>
    Task SoftDeleteAsync(string id);
}
