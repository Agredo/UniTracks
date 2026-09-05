namespace UniTracks.Games.Catalog;

/// <summary>
/// Static registry of all mini games. Pure data — resolving a game to a page
/// happens in the UI layer via <see cref="GameInfo.Route"/>.
/// </summary>
public static class GameCatalog
{
    public const string CityBuilderId = "city-builder";

    public static IReadOnlyList<GameInfo> Games { get; } = new List<GameInfo>
    {
        new()
        {
            Id = CityBuilderId,
            Title = "Cozy City",
            Description = "Baue deine eigene kleine Stadt — finanziert durch deine Trips und Erfolge.",
            Icon = "🏙️",
            Route = "CityBuilderPage",
        },
        new()
        {
            Id = "tower-defense",
            Title = "Trail Defense",
            Description = "Verteidige deinen Trail gegen fiese Mücken. Bald verfügbar!",
            Icon = "🗼",
            IsAvailable = false,
        },
    };

    public static GameInfo? Find(string id) => Games.FirstOrDefault(g => g.Id == id);
}
