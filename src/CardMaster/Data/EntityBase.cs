using SQLite;

namespace CardMaster.Data;

/// <summary>
/// Base per tutte le entità persistite. Vincoli architetturali v1:
/// - <see cref="Id"/> generato dal CLIENT (GUID) — resta valido con la futura sync (v2).
/// - Cancellazione LOGICA via tombstone (<see cref="DeletedAt"/>): mai DELETE fisici.
/// </summary>
public abstract class EntityBase
{
    /// <summary>Identificativo generato dal client (GUID). Chiave primaria.</summary>
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Tombstone: se valorizzato l'entità è cancellata logicamente.</summary>
    [Indexed]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Vero se l'entità è un tombstone (cancellata logicamente).</summary>
    [Ignore]
    public bool IsDeleted => DeletedAt.HasValue;
}
