namespace UniTracks.Games.Persistence;

/// <summary>
/// A purchased city-grid expansion. Persisted via the provider-agnostic repository
/// (EF Core + SQLite / LiteDB on iOS) — the grid size is derived from the contiguous
/// progression in <c>CityExpansions</c>, never stored redundantly.
/// </summary>
public class CityExpansion
{
    public Guid ID { get; set; }

    /// <summary>Grid edge length this purchase unlocked (8, 10, 12).</summary>
    public int GridSize { get; set; }

    public DateTimeOffset PurchasedAt { get; set; }
}
