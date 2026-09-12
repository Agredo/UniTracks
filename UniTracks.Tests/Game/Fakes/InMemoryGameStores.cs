using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Tests.Game.Fakes;

/// <summary>In-memory <see cref="ICityStore"/> — same write-through semantics as the real store.</summary>
internal sealed class InMemoryCityStore : ICityStore
{
    private readonly List<PlacedBuilding> placed = new();
    private readonly List<CityExpansion> expansions = new();

    public Task<IReadOnlyList<PlacedBuilding>> LoadAsync() =>
        Task.FromResult<IReadOnlyList<PlacedBuilding>>(placed.ToList());

    public Task SaveAsync(PlacedBuilding building)
    {
        placed.Add(building);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlacedBuilding building)
    {
        placed.RemoveAll(p => p.ID == building.ID);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CityExpansion>> LoadExpansionsAsync() =>
        Task.FromResult<IReadOnlyList<CityExpansion>>(expansions.ToList());

    public Task SaveExpansionAsync(CityExpansion expansion)
    {
        expansions.Add(expansion);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory <see cref="IActivityStatsSource"/> returning a fixed lifetime snapshot.</summary>
internal sealed class FakeActivityStatsSource : IActivityStatsSource
{
    public FakeActivityStatsSource(ActivityStats stats) => Stats = stats;

    public ActivityStats Stats { get; set; }

    public Task<ActivityStats> GetAsync() => Task.FromResult(Stats);
}
