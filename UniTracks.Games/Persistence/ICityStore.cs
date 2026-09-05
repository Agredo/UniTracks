namespace UniTracks.Games.Persistence;

/// <summary>
/// Persistence port for the city builder. Implemented in UniTracks.Services on top of
/// the provider-agnostic <c>IRepository</c> (EF Core + SQLite, LiteDB on iOS).
/// </summary>
public interface ICityStore
{
    Task<IReadOnlyList<PlacedBuilding>> LoadAsync();

    Task SaveAsync(PlacedBuilding building);

    Task DeleteAsync(PlacedBuilding building);

    Task<IReadOnlyList<CityExpansion>> LoadExpansionsAsync();

    Task SaveExpansionAsync(CityExpansion expansion);
}
