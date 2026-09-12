using UniTracks.Games.CityBuilder;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.Shared.Economy;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense;
using UniTracks.Games.TowerDefense.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Single source of truth for the shared coin account: sums what the player earned from real
/// activity and what every game has spent. The balance is computed on every read and never
/// stored, so it can neither drift out of sync with the trip data nor between the games.
/// </summary>
public class CoinAccountService : ICoinAccountService
{
    private readonly ICityStore cityStore;
    private readonly ITowerDefenseStore towerDefenseStore;
    private readonly IActivityStatsSource activityStats;

    public CoinAccountService(ICityStore cityStore, ITowerDefenseStore towerDefenseStore, IActivityStatsSource activityStats)
    {
        this.cityStore = cityStore;
        this.towerDefenseStore = towerDefenseStore;
        this.activityStats = activityStats;
    }

    public async Task<CoinAccount> GetAsync()
    {
        var stats = await activityStats.GetAsync();
        var placed = await cityStore.LoadAsync();
        var expansions = await cityStore.LoadExpansionsAsync();
        var unlocks = await towerDefenseStore.LoadUnlocksAsync();
        var energyPurchases = await towerDefenseStore.LoadEnergyPurchasesAsync();

        return new CoinAccount
        {
            Earned = CoinEconomy.ComputeEarned(stats.Trips, stats.Xp, stats.UnlockedAchievements),
            CitySpent = CityEngine.ComputeTotalSpent(placed, expansions),
            TowerDefenseSpent = DefenseEngine.ComputeTotalSpent(unlocks, energyPurchases),
        };
    }
}
