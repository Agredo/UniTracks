using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Services.Game;
using UniTracks.Tests.Game.Fakes;
using UniTracks.Tests.TowerDefense.Fakes;

namespace UniTracks.Tests.Game;

/// <summary>
/// The coin account is shared by both games: what the city builder reports as spendable
/// must be the same number the catalog (and therefore the tower defense shop) reports.
/// </summary>
public class SharedCoinBalanceTests
{
    private static ActivityStats FortyKmRun => new()
    {
        Trips = new[] { new TripActivity { DistanceKm = 40, Category = "running", Identifier = "run" } },
        Xp = 200,
    };

    [Fact]
    public async Task CityBuilderBalance_MatchesTheSharedBalance_AcrossBothGames()
    {
        // 500 welcome + 400 distance (40 km × 1,0 × 10) + 5 trip + 500 level bonus = 1405 earned.
        var stats = FortyKmRun;
        var cityStore = new InMemoryCityStore();
        var towerStore = new InMemoryTowerDefenseStore();
        var statsSource = new FakeActivityStatsSource(stats);
        var account = new CoinAccountService(cityStore, towerStore, statsSource);

        var cityBuilder = new CityBuilderService(cityStore, statsSource, account);
        await towerStore.SaveUnlockAsync(new TowerUnlock { ID = Guid.NewGuid(), TowerId = "zapper" });
        await towerStore.SaveEnergyPurchaseAsync(new EnergyPurchase { ID = Guid.NewGuid(), Energy = 50, Coins = 100 });
        await cityBuilder.TryPlaceAsync("flowerbed", 0, 0);

        int shared = await new GameCatalogService(account, statsSource).GetCoinBalanceAsync();
        var city = await cityBuilder.GetCityAsync();

        Assert.True(city.CoinsSpent > 0, "the placed building must count as city spending");
        Assert.Equal(shared, city.Coins);
    }

    [Fact]
    public async Task TowerDefenseSpending_ReducesTheCityBalance()
    {
        var cityStore = new InMemoryCityStore();
        var towerStore = new InMemoryTowerDefenseStore();
        var statsSource = new FakeActivityStatsSource(FortyKmRun);
        var account = new CoinAccountService(cityStore, towerStore, statsSource);
        var cityBuilder = new CityBuilderService(cityStore, statsSource, account);

        int before = (await cityBuilder.GetCityAsync()).Coins;
        await towerStore.SaveUnlockAsync(new TowerUnlock { ID = Guid.NewGuid(), TowerId = "zapper" });
        int after = (await cityBuilder.GetCityAsync()).Coins;

        Assert.Equal(before - 400, after);
    }

    [Fact]
    public async Task CitySpending_ReducesTheTowerDefenseBalance()
    {
        var cityStore = new InMemoryCityStore();
        var towerStore = new InMemoryTowerDefenseStore();
        var statsSource = new FakeActivityStatsSource(FortyKmRun);
        var account = new CoinAccountService(cityStore, towerStore, statsSource);
        var cityBuilder = new CityBuilderService(cityStore, statsSource, account);
        var catalog = new GameCatalogService(account, statsSource);

        int before = await catalog.GetCoinBalanceAsync();
        await cityBuilder.TryPlaceAsync("flowerbed", 0, 0);
        int after = await catalog.GetCoinBalanceAsync();

        Assert.Equal(before - 15, after);
    }

    [Fact]
    public async Task CityBuilder_CannotSpendCoinsAlreadySpentInTowerDefense()
    {
        var cityStore = new InMemoryCityStore();
        var towerStore = new InMemoryTowerDefenseStore();
        var statsSource = new FakeActivityStatsSource(FortyKmRun);
        var account = new CoinAccountService(cityStore, towerStore, statsSource);
        var cityBuilder = new CityBuilderService(cityStore, statsSource, account);

        // 1405 earned − 650 unlock = 755 left: the 15-coin flowerbed is still affordable.
        await towerStore.SaveUnlockAsync(new TowerUnlock { ID = Guid.NewGuid(), TowerId = "frog" });

        var result = await cityBuilder.TryPlaceAsync("flowerbed", 0, 0);
        var city = await cityBuilder.GetCityAsync();

        Assert.True(result.Success, "a 15-coin building must still be affordable");
        Assert.Equal(1405 - 650 - 15, city.Coins);
    }
}
