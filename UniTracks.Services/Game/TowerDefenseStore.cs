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

    public async Task<DefenseRecord?> LoadRecordAsync() =>
        (await repository.GetAllAsync<DefenseRecord>()).FirstOrDefault();

    public Task SaveRecordAsync(DefenseRecord record) => repository.Update(record);
}
