namespace UniTracks.Games.CityBuilder;

/// <summary>Theme of a building — used by the renderer to pick colors and shapes.</summary>
public enum BuildingTheme
{
    Nature,
    Water,
    Residential,
    Commercial,
    Leisure,
    Civic,
}

/// <summary>A purchasable building type from the static <see cref="BuildingCatalog"/>.</summary>
public record BuildingDefinition
{
    /// <summary>Stable identifier, e.g. "tree" — persisted on placed buildings.</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>Fallback glyph for non-Skia UI (shop chips, lists).</summary>
    public string Icon { get; init; } = "🏠";

    public int Cost { get; init; }

    public BuildingTheme Theme { get; init; } = BuildingTheme.Residential;

    /// <summary>Gamification level needed to unlock this building in the shop (1 = always available).</summary>
    public int RequiredLevel { get; init; } = 1;

    /// <summary>Achievement id that must be unlocked first (exclusive prestige buildings), or null.</summary>
    public string? RequiredAchievementId { get; init; }
}
