using UniTracks.Data.Repository;
using UniTracks.Games.TowerDefense.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Implements the trail-defense persistence port on top of the provider-agnostic
/// repository (EF Core + SQLite on most platforms, LiteDB on iOS).
/// </summary>
public class TowerDefenseStore : ITowerDefenseStore
{
    private readonly IRepository repository;

    public TowerDefenseStore(IRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyList<TowerUnlock>> LoadUnlocksAsync() =>
        (await repository.GetAllAsync<TowerUnlock>()).ToList();

    public Task SaveUnlockAsync(TowerUnlock unlock) => repository.Add(unlock);

    public async Task<IReadOnlyList<EnergyPurchase>> LoadEnergyPurchasesAsync() =>
        (await repository.GetAllAsync<EnergyPurchase>()).ToList();

    public Task SaveEnergyPurchaseAsync(EnergyPurchase purchase) => repository.Add(purchase);

    public async Task<DefenseRecord?> LoadRecordAsync() =>
        (await repository.GetAllAsync<DefenseRecord>()).FirstOrDefault();

    public async Task SaveRecordAsync(DefenseRecord record)
    {
        var existing = (await repository.GetAllAsync<DefenseRecord>()).FirstOrDefault();
        if (existing is null)
        {
            await repository.Add(record);
            return;
        }

        existing.BestWave = record.BestWave;
        existing.BestScore = record.BestScore;
        existing.UpdatedAt = record.UpdatedAt;
        await repository.Update(existing);
    }

    public async Task<DefenseRunProgress?> LoadRunAsync() =>
        (await repository.GetAllAsync<DefenseRunProgress>()).FirstOrDefault();

    public async Task SaveRunAsync(DefenseRunProgress run)
    {
        var existing = (await repository.GetAllAsync<DefenseRunProgress>()).FirstOrDefault();
        if (existing is null)
        {
            await repository.Add(run);
            return;
        }

        // Mutate the already-tracked instance instead of creating a new one: EF Core
        // throws an InvalidOperationException (IdentityConflict) when two instances share
        // the same key, which surfaced as the 0xc000027b stowed exception on placement.
        existing.Wave = run.Wave;
        existing.Energy = run.Energy;
        existing.Lives = run.Lives;
        existing.Score = run.Score;
        existing.TowersJson = run.TowersJson;
        existing.UpdatedAt = run.UpdatedAt;
        await repository.Update(existing);
    }

    public async Task ClearRunAsync()
    {
        var existing = (await repository.GetAllAsync<DefenseRunProgress>()).FirstOrDefault();
        if (existing is not null)
        {
            await repository.Delete(existing);
        }
    }
}
