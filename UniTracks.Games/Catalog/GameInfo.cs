namespace UniTracks.Games.Catalog;

/// <summary>
/// Describes an available mini game in the games tab. The catalog is extensible:
/// new games are registered in <see cref="GameCatalog"/> without touching the UI.
/// </summary>
public record GameInfo
{
    /// <summary>Stable identifier, e.g. "city-builder".</summary>
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = "🎮";

    /// <summary>Shell route used to navigate into the game (registered in App.xaml.cs).</summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>False for catalog placeholders that are announced but not playable yet.</summary>
    public bool IsAvailable { get; init; } = true;
}
