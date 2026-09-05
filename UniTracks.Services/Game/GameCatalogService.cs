using UniTracks.Games.Catalog;
using UniTracks.Games.CityBuilder;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.Shared.Economy;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense;
using UniTracks.Games.TowerDefense.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Game registry plus the shared coin balance: coins are earned from activity and
/// spent across all games (city buildings, expansions, tower unlocks) — always
/// computed, never stored, so cross-game spending can never drift apart.
/// </summary>
public class GameCatalogService : IGameCatalogService
{
    private readonly ICityStore cityStore;
    private readonly ITowerDefenseStore towerDefenseStore;
    private readonly IActivityStatsSource activityStats;

    public GameCatalogService(ICityStore cityStore, ITowerDefenseStore towerDefenseStore, IActivityStatsSource activityStats)
    {
        this.cityStore = cityStore;
        this.towerDefenseStore = towerDefenseStore;
        this.activityStats = activityStats;
    }

    public IReadOnlyList<GameInfo> GetGames() => GameCatalog.Games;

    public async Task<int> GetCoinBalanceAsync()
    {
        var stats = await activityStats.GetAsync();
        var placed = await cityStore.LoadAsync();
        var expansions = await cityStore.LoadExpansionsAsync();
        var unlocks = await towerDefenseStore.LoadUnlocksAsync();
        int earned = CoinEconomy.ComputeEarned(stats.Trips, stats.Xp, stats.UnlockedAchievements);
        int spent = CityEngine.ComputeSpent(placed)
            + CityEngine.ComputeExpansionSpent(expansions)
            + DefenseEngine.ComputeUnlockSpent(unlocks);
        return Math.Max(0, earned - spent);
    }
}
