using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.Persistence;

/// <summary>
/// A building placed on the city map. This is the only persisted game state —
/// coin balance is always computed (earned from activity minus spent on buildings).
/// Works on EF Core (SQLite) as well as LiteDB on iOS (requires an ID property).
/// </summary>
public record PlacedBuilding
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>References <c>BuildingDefinition.Id</c> from the static catalog.</summary>
    public string BuildingId { get; init; } = string.Empty;

    /// <summary>Tile coordinates on the city grid.</summary>
    public int X { get; init; }
    public int Y { get; init; }

    public DateTimeOffset PlacedAt { get; init; } = DateTimeOffset.UtcNow;
}
