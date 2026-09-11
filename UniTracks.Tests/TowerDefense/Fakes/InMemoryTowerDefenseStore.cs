using UniTracks.Games.Catalog;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Services.Game;

namespace UniTracks.Tests.TowerDefense.Fakes;

/// <summary>
/// In-memory <see cref="ITowerDefenseStore"/>. Mirrors the one behavioural detail of the real
/// store that the service relies on: a run snapshot is a single row that is updated in place,
/// so its <see cref="DefenseRunProgress.ID"/> never changes.
/// </summary>
internal sealed class InMemoryTowerDefenseStore : ITowerDefenseStore
{
    private readonly List<TowerUnlock> unlocks = new();
    private readonly List<EnergyPurchase> purchases = new();
    private DefenseRecord? record;
    private DefenseRunProgress? run;

    public IReadOnlyList<TowerUnlock> Unlocks => unlocks;

    public IReadOnlyList<EnergyPurchase> Purchases => purchases;

    public DefenseRecord? Record => record;

    public DefenseRunProgress? Run => run;

    public int SaveRunCalls { get; private set; }

    /// <summary>Puts a snapshot into the store as if a previous session had written it.</summary>
    public void SeedRun(DefenseRunProgress progress) => run = progress;

    public Task<IReadOnlyList<TowerUnlock>> LoadUnlocksAsync() =>
        Task.FromResult<IReadOnlyList<TowerUnlock>>(unlocks.ToList());

    public Task SaveUnlockAsync(TowerUnlock unlock)
    {
        unlocks.Add(unlock);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EnergyPurchase>> LoadEnergyPurchasesAsync() =>
        Task.FromResult<IReadOnlyList<EnergyPurchase>>(purchases.ToList());

    public Task SaveEnergyPurchaseAsync(EnergyPurchase purchase)
    {
        purchases.Add(purchase);
        return Task.CompletedTask;
    }

    public Task<DefenseRecord?> LoadRecordAsync() => Task.FromResult(record);

    public Task SaveRecordAsync(DefenseRecord value)
    {
        record = value;
        return Task.CompletedTask;
    }

    public Task<DefenseRunProgress?> LoadRunAsync() => Task.FromResult(run);

    public Task SaveRunAsync(DefenseRunProgress value)
    {
        SaveRunCalls++;

        if (run is null)
        {
            run = value;
            return Task.CompletedTask;
        }

        // Mirrors the real store field-for-field: MapId, BestClearWave and BestClearScore are
        // deliberately not updated on an existing row (known open finding), so the fake must not
        // "fix" that here or the round-trip tests would assert behaviour production does not have.
        run.Wave = value.Wave;
        run.Energy = value.Energy;
        run.Lives = value.Lives;
        run.Score = value.Score;
        run.TowersJson = value.TowersJson;
        run.UpdatedAt = value.UpdatedAt;
        return Task.CompletedTask;
    }

    public Task ClearRunAsync()
    {
        run = null;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Stub for the catalog port. The tower-defense service only reads the coin balance and the
/// lifetime activity stats, so those are the only members that carry state.
/// </summary>
internal sealed class FakeGameCatalogService : IGameCatalogService
{
    public int Coins { get; set; }

    public ActivityStats Stats { get; set; } = new();

    public IReadOnlyList<GameInfo> Games { get; set; } = Array.Empty<GameInfo>();

    public IReadOnlyList<GameInfo> GetGames() => Games;

    public Task<int> GetCoinBalanceAsync() => Task.FromResult(Coins);

    public Task<ActivityStats> GetActivityStatsAsync() => Task.FromResult(Stats);
}
