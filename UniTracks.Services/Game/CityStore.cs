using UniTracks.Data.Repository;
using UniTracks.Games.CityBuilder.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Implements the games-layer persistence port on top of the provider-agnostic
/// repository (EF Core + SQLite on most platforms, LiteDB on iOS).
/// </summary>
public class CityStore : ICityStore
{
    private readonly IRepository repository;

    public CityStore(IRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyList<PlacedBuilding>> LoadAsync() =>
        (await repository.GetAllAsync<PlacedBuilding>()).ToList();

    public Task SaveAsync(PlacedBuilding building) => repository.Add(building);

    public Task DeleteAsync(PlacedBuilding building) => repository.Delete(building);

    public async Task<IReadOnlyList<CityExpansion>> LoadExpansionsAsync() =>
        (await repository.GetAllAsync<CityExpansion>()).ToList();

    public Task SaveExpansionAsync(CityExpansion expansion) => repository.Add(expansion);
}
