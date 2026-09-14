using UniTracks.Data.Repository;
using UniTracks.Games.BaseCamp.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Implements the base-camp persistence port on top of the provider-agnostic repository
/// (EF Core + SQLite on most platforms, LiteDB on iOS).
/// </summary>
public class CampStore : ICampStore
{
    private readonly IRepository repository;

    public CampStore(IRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyList<CampModule>> LoadModulesAsync() =>
        (await repository.GetAllAsync<CampModule>()).ToList();

    public async Task SaveModuleAsync(CampModule module)
    {
        var existing = (await repository.GetAllAsync<CampModule>())
            .FirstOrDefault(m => m.ModuleId == module.ModuleId);

        if (existing is null)
        {
            await repository.Add(module);
            return;
        }

        // Mutate the already-tracked instance instead of adding a second one: EF Core throws
        // an InvalidOperationException (IdentityConflict) when two instances share the same key.
        existing.Level = module.Level;
        existing.UpdatedAt = module.UpdatedAt;
        await repository.Update(existing);
    }

    public async Task<CampLog?> LoadLogAsync() =>
        (await repository.GetAllAsync<CampLog>()).FirstOrDefault();

    public async Task SaveLogAsync(CampLog log)
    {
        var existing = (await repository.GetAllAsync<CampLog>()).FirstOrDefault();
        if (existing is null)
        {
            await repository.Add(log);
            return;
        }

        // Same reason as above: the camp keeps exactly one log row, and it is updated in place.
        existing.LastCollectedAt = log.LastCollectedAt;
        existing.TotalCollected = log.TotalCollected;
        existing.BankedSupplies = log.BankedSupplies;
        existing.UpdatedAt = log.UpdatedAt;
        await repository.Update(existing);
    }
}
