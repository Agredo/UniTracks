namespace UniTracks.Games.CityBuilder;

/// <summary>A single tile of the city grid — empty or occupied by a placed building.</summary>
public record CityTile
{
    public int X { get; init; }

    public int Y { get; init; }

    /// <summary>Id of the placed building occupying this tile, or null when empty.</summary>
    public Guid? PlacedBuildingId { get; init; }

    /// <summary>Catalog id of the building (for rendering), or null when empty.</summary>
    public string? BuildingId { get; init; }

    /// <summary>UTC timestamp of placement — used for the drop-in animation.</summary>
    public DateTimeOffset? PlacedAt { get; init; }

    public bool IsEmpty => PlacedBuildingId is null;
}
