using UniTracks.Games.Catalog;
using UniTracks.Games.CityBuilder;
using UniTracks.Games.Economy;
using UniTracks.Games.Persistence;

namespace UniTracks.Services.Game;

public class GameCatalogService : IGameCatalogService
{
    private readonly ICityStore cityStore;
    private readonly IActivityStatsSource activityStats;

    public GameCatalogService(ICityStore cityStore, IActivityStatsSource activityStats)
    {
        this.cityStore = cityStore;
        this.activityStats = activityStats;
    }

    public IReadOnlyList<GameInfo> GetGames() => GameCatalog.Games;

    public async Task<int> GetCoinBalanceAsync()
    {
        var stats = await activityStats.GetAsync();
        var placed = await cityStore.LoadAsync();
        var expansions = await cityStore.LoadExpansionsAsync();
        int earned = CoinEconomy.ComputeEarned(stats.Trips, stats.Xp, stats.UnlockedAchievements);
        return Math.Max(0, earned - CityEngine.ComputeSpent(placed) - CityEngine.ComputeExpansionSpent(expansions));
    }
}
