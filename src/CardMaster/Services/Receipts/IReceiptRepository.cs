using CardMaster.Data;

namespace CardMaster.Services.Receipts;

/// <summary>
/// Accesso agli scontrini nel database locale. Cancellazione sempre <b>logica</b> (tombstone),
/// come per le carte: mai DELETE fisici, per non complicare la sincronizzazione di v2.
/// </summary>
public interface IReceiptRepository
{
    /// <summary>Scontrini attivi, dal più recente per data d'acquisto (o di creazione se manca).</summary>
    Task<List<Receipt>> GetAllAsync();

    /// <summary>Uno scontrino attivo per Id, o null se assente o cancellato.</summary>
    Task<Receipt?> GetByIdAsync(string id);

    /// <summary>Inserisce un nuovo scontrino, assegnando Id e timestamp.</summary>
    Task AddAsync(Receipt receipt);

    /// <summary>Aggiorna uno scontrino esistente e ne rinfresca <c>UpdatedAt</c>.</summary>
    Task UpdateAsync(Receipt receipt);

    /// <summary>
    /// Marca lo scontrino come cancellato (tombstone). L'eventuale immagine associata va
    /// rimossa dal device a parte: il repository non tocca il filesystem.
    /// </summary>
    Task DeleteAsync(string id);
}
