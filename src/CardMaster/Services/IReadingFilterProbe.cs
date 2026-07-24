namespace CardMaster.Services;

/// <summary>Stato (best-effort) del filtro luce blu / modalità notte di sistema.</summary>
public enum ReadingFilterState
{
    /// <summary>Filtro attivo (rilevato).</summary>
    Active,

    /// <summary>Filtro non attivo (rilevato).</summary>
    Inactive,

    /// <summary>Stato non determinabile su questo dispositivo.</summary>
    Unknown,
}

/// <summary>
/// Rileva, per quanto possibile, se è attivo un filtro luce blu di sistema.
/// Best-effort: su OEM che non espongono l'impostazione restituisce <see cref="ReadingFilterState.Unknown"/>.
/// </summary>
public interface IReadingFilterProbe
{
    ReadingFilterState Probe();
}
