using UniTracks.Games.BaseCamp.Persistence;
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

/// <summary>
/// In-memory <see cref="ICampStore"/> with the same single-row-per-module semantics as the real
/// store: saving an existing module updates it in place instead of appending a second row.
/// </summary>
internal sealed class InMemoryCampStore : ICampStore
{
    private readonly List<CampModule> modules = new();
    private CampLog? log;

    public int ModuleRowCount => modules.Count;

    public Task<IReadOnlyList<CampModule>> LoadModulesAsync() =>
        Task.FromResult<IReadOnlyList<CampModule>>(modules.ToList());

    public Task SaveModuleAsync(CampModule module)
    {
        var existing = modules.FirstOrDefault(m => m.ModuleId == module.ModuleId);
        if (existing is null)
        {
            modules.Add(module);
            return Task.CompletedTask;
        }

        existing.Level = module.Level;
        existing.UpdatedAt = module.UpdatedAt;
        return Task.CompletedTask;
    }

    public Task<CampLog?> LoadLogAsync() => Task.FromResult(log);

    public Task SaveLogAsync(CampLog value)
    {
        if (log is null)
        {
            log = value;
            return Task.CompletedTask;
        }

        log.LastCollectedAt = value.LastCollectedAt;
        log.TotalCollected = value.TotalCollected;
        log.BankedSupplies = value.BankedSupplies;
        log.UpdatedAt = value.UpdatedAt;
        return Task.CompletedTask;
    }
}
