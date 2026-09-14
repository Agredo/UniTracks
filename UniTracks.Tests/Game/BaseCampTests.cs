using UniTracks.Games.BaseCamp;
using UniTracks.Games.BaseCamp.Persistence;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Services.Game;
using UniTracks.Tests.Game.Fakes;
using UniTracks.Tests.TowerDefense.Fakes;

namespace UniTracks.Tests.Game;

/// <summary>
/// The base camp is an idle game, but its supplies must never grow on pure waiting:
/// production is driven by the player's real trips, decays during pauses and is capped by
/// the camp's capacity. Only module levels and the camp's ledger are persisted.
/// </summary>
public class BaseCampTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);

    private static ActivityStats StatsWith(params TripActivity[] trips) => new() { Trips = trips };

    private static TripActivity Run(double km, DateTimeOffset startedAt) =>
        new() { DistanceKm = km, StartedAt = startedAt, Category = "running", Identifier = "run" };

    private static TripActivity EBike(double km, DateTimeOffset startedAt) =>
        new() { DistanceKm = km, StartedAt = startedAt, Category = "cycling", Identifier = "ebikeride" };

    private static IReadOnlyList<CampModuleLevel> Levels(params (string Id, int Level)[] levels) =>
        levels.Select(l => new CampModuleLevel { ModuleId = l.Id, Level = l.Level }).ToList();

    [Fact]
    public void NewCamp_StartsWithTheWelcomeStockAndTheBaseRate()
    {
        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), null, StatsWith(), Now);

        Assert.Equal(CampEconomy.StartingSupplies, camp.Supplies);
        Assert.Equal(CampEconomy.BaseCapacity, camp.Capacity);
        Assert.Equal(CampEconomy.BaseRatePerHour, camp.RatePerHour, 3);
        Assert.False(camp.IsFull);

        // The welcome stock is spendable right away, but it is not lying in the camp: the camp
        // itself only starts filling up once the first visit has anchored it.
        Assert.Equal(0, camp.Stock);
    }

    [Fact]
    public void WeeklyKilometres_RaiseTheProductionRate()
    {
        var stats = StatsWith(Run(40, Now.AddHours(-3)));

        // 6 base + 0,8 × 40 weighted km = 38/h.
        Assert.Equal(38.0, CampEconomy.ComputeRate(stats, Array.Empty<CampModuleLevel>(), Now), 3);
    }

    [Fact]
    public void Streak_RaisesTheProductionRate()
    {
        var stats = new ActivityStats { CurrentStreakDays = 4 };

        // 6 base × (1 + 0,10 × 4 Tage) = 8,4/h.
        Assert.Equal(8.4, CampEconomy.ComputeRate(stats, Array.Empty<CampModuleLevel>(), Now), 3);
    }

    [Fact]
    public void TripTypeFactor_MakesAnEBikeWorthLessThanARun()
    {
        var run = StatsWith(Run(40, Now.AddHours(-3)));
        var ebike = StatsWith(EBike(40, Now.AddHours(-3)));

        double runRate = CampEconomy.ComputeRate(run, Array.Empty<CampModuleLevel>(), Now);
        double ebikeRate = CampEconomy.ComputeRate(ebike, Array.Empty<CampModuleLevel>(), Now);

        Assert.True(ebikeRate < runRate, "a motor-assisted trip must produce less than a run of the same distance");
        Assert.Equal(9.2, ebikeRate, 3); // 6 base + 0,8 × (40 × 0,1)
    }

    [Fact]
    public void Pause_DecaysTheRateDownToTheFreshnessFloor()
    {
        var stats = StatsWith(Run(40, Now.AddDays(-5)));

        // 38/h × 0,12 (five days without a trip, base floor 0,08) — far below the 6/h base rate.
        Assert.Equal(4.56, CampEconomy.ComputeRate(stats, Array.Empty<CampModuleLevel>(), Now), 3);
    }

    [Fact]
    public void Cellar_RaisesTheFloorAfterAPause()
    {
        var stats = StatsWith(Run(40, Now.AddDays(-5)));
        var modules = Levels((CampCatalog.CellarId, 3));

        // Floor raised from 0,08 to 0,40: 38/h × 0,40.
        Assert.Equal(15.2, CampEconomy.ComputeRate(stats, modules, Now), 3);
    }

    [Fact]
    public void Capacity_CapsWhatASingleHarvestCanPayOut()
    {
        var stats = StatsWith(Run(40, Now.AddHours(-50)));
        var log = new CampLog { ID = Guid.NewGuid(), LastCollectedAt = Now.AddHours(-48) };

        // Far more than 400 supplies accrue in 48 h, but the camp only holds 400.
        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), log, stats, Now);

        Assert.Equal(camp.Capacity, camp.Stock);
        Assert.True(camp.IsFull);

        // The cap applies to production only — the balance the player owns is unaffected.
        Assert.Equal(0, camp.Supplies);
    }

    [Fact]
    public void CatchUpWindow_IsLimitedToFortyEightHours()
    {
        double accrued = CampEconomy.ComputeAccrued(StatsWith(), Array.Empty<CampModuleLevel>(), Now.AddDays(-10), Now);

        Assert.Equal(48 * CampEconomy.BaseRatePerHour, accrued, 3);
    }

    [Fact]
    public void ClockSetBackwards_PaysOutNothing()
    {
        double accrued = CampEconomy.ComputeAccrued(StatsWith(), Array.Empty<CampModuleLevel>(), Now.AddHours(2), Now);

        Assert.Equal(0, accrued, 3);
    }

    [Fact]
    public void Harvest_MovesTheStockIntoTheBalanceAndLeavesTheCampEmpty()
    {
        var stats = StatsWith(Run(40, Now.AddHours(-50)));
        var log = new CampLog
        {
            ID = Guid.NewGuid(),
            LastCollectedAt = Now,
            TotalCollected = 400,
            BankedSupplies = 400,
        };

        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), log, stats, Now);

        // The welcome stock is granted once, and a harvest empties the camp: only new activity
        // fills it up again. What was harvested stays spendable.
        Assert.Equal(0, camp.Stock);
        Assert.Equal(400, camp.Supplies);
        Assert.Equal(Now, camp.LastCollectedAt);
        Assert.Equal(400, camp.TotalCollected);
    }

    [Fact]
    public void PartialHour_DoesNotPayOutYetAndIsKeptForTheNextHarvest()
    {
        double accrued = CampEconomy.ComputeAccrued(StatsWith(), Array.Empty<CampModuleLevel>(), Now, Now.AddMinutes(59));

        Assert.Equal(0, accrued, 3);

        // The anchor only advances by the paid-out hours, so the 59 minutes are not lost.
        Assert.Equal(Now, CampEconomy.ComputeNextAnchor(Now, Now.AddMinutes(59)));
        Assert.Equal(Now.AddHours(1), CampEconomy.ComputeNextAnchor(Now, Now.AddHours(1).AddMinutes(59)));
    }

    [Fact]
    public void Kitchen_RaisesTheBaseRate()
    {
        var modules = Levels((CampCatalog.KitchenId, 1));

        // 6 × 1,25 = 7,5/h without any activity.
        Assert.Equal(7.5, CampEconomy.ComputeRate(StatsWith(), modules, Now), 3);
    }

    [Fact]
    public void Tent_RaisesTheCapacity()
    {
        var modules = Levels((CampCatalog.TentId, 4));

        // 400 × 1,5⁴ = 2025.
        Assert.Equal(2025, CampEconomy.ComputeCapacity(modules));
    }

    [Fact]
    public void Map_WeightsWeeklyKilometresMore()
    {
        var stats = StatsWith(Run(40, Now.AddHours(-3)));
        var modules = Levels((CampCatalog.MapId, 1));

        // 40 km × 1,2 = 48 weighted km → 6 + 38,4 = 44,4/h.
        Assert.Equal(44.4, CampEconomy.ComputeRate(stats, modules, Now), 3);
    }

    [Fact]
    public void Radio_RaisesTheStreakFactor()
    {
        var stats = new ActivityStats { CurrentStreakDays = 4 };
        var modules = Levels((CampCatalog.RadioId, 1));

        // 6 × (1 + 0,4 + 0,05) = 8,7/h.
        Assert.Equal(8.7, CampEconomy.ComputeRate(stats, modules, Now), 3);
    }

    [Fact]
    public void ComputeSpent_CountsEveryPurchasedLevel()
    {
        // Coins only: map levels 1–3 cost 300 + 700 + 1500.
        Assert.Equal(2500, CampEngine.ComputeSpent(Levels((CampCatalog.MapId, 3))));

        // Supplies-only modules never touch the shared coin balance.
        Assert.Equal(0, CampEngine.ComputeSpent(Levels((CampCatalog.TentId, 4), (CampCatalog.KitchenId, 4))));

        // Mixed module: cellar levels 1–2 cost 300 + 700 coins.
        Assert.Equal(1000, CampEngine.ComputeSpent(Levels((CampCatalog.CellarId, 2))));
    }

    [Fact]
    public void Upgrade_IsAffordableFromTheWelcomeStock()
    {
        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), null, StatsWith(), Now);

        var result = CampEngine.ValidateUpgrade(camp, CampCatalog.TentId);

        Assert.True(result.Success);
        Assert.Equal(150, result.Cost.Supplies);
        Assert.Equal(0, result.Cost.Coins);
    }

    [Fact]
    public void Upgrade_FailsWithoutEnoughSupplies()
    {
        var modules = new[] { new CampModule { ID = Guid.NewGuid(), ModuleId = CampCatalog.KitchenId, Level = 3 } };
        var camp = CampEngine.Rebuild(modules, null, StatsWith(), Now);

        // 250 supplies in stock, the fourth kitchen level costs 1900.
        var result = CampEngine.ValidateUpgrade(camp, CampCatalog.KitchenId);

        Assert.False(result.Success);
        Assert.Contains("Vorräte", result.ErrorMessage);
    }

    [Fact]
    public void Upgrade_FailsWithoutEnoughCoins()
    {
        var stats = new ActivityStats
        {
            Trips = new[] { Run(40, Now.AddHours(-3)) },
            Xp = 200,
        };
        var modules = new[] { new CampModule { ID = Guid.NewGuid(), ModuleId = CampCatalog.MapId, Level = 1 } };
        var camp = CampEngine.Rebuild(modules, null, stats, Now);

        // 500 welcome + 400 distance + 5 trip + 500 level bonus − 300 already spent on the map.
        Assert.Equal(1105, camp.Coins);
        Assert.True(CampEngine.ValidateUpgrade(camp, CampCatalog.MapId).Success);

        var builtUp = new[] { new CampModule { ID = Guid.NewGuid(), ModuleId = CampCatalog.MapId, Level = 2 } };
        var result = CampEngine.ValidateUpgrade(CampEngine.Rebuild(builtUp, null, stats, Now), CampCatalog.MapId);

        // Level 3 costs 1500 coins, only 405 are left.
        Assert.False(result.Success);
        Assert.Contains("Coins", result.ErrorMessage);
    }

    [Fact]
    public void Upgrade_FailsWhenTheLevelGateIsNotReached()
    {
        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), null, StatsWith(), Now);

        var result = CampEngine.ValidateUpgrade(camp, CampCatalog.CellarId);

        Assert.False(result.Success);
        Assert.Contains("Level 2", result.ErrorMessage);
    }

    [Fact]
    public void Upgrade_FailsWhenTheAchievementGateIsNotReached()
    {
        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), null, StatsWith(), Now);

        var result = CampEngine.ValidateUpgrade(camp, CampCatalog.FlagId);

        Assert.False(result.Success);
        Assert.Contains("Erfolg", result.ErrorMessage);
    }

    [Fact]
    public void Upgrade_IsRejectedOnceTheModuleIsMaxedOut()
    {
        var modules = new[] { new CampModule { ID = Guid.NewGuid(), ModuleId = CampCatalog.TentId, Level = 4 } };
        var camp = CampEngine.Rebuild(modules, null, StatsWith(), Now);

        var result = CampEngine.ValidateUpgrade(camp, CampCatalog.TentId);

        Assert.False(result.Success);
        Assert.Contains("voll ausgebaut", result.ErrorMessage);
    }

    [Fact]
    public void Upgrade_ReportsTheReachedLevel()
    {
        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), null, StatsWith(), Now);

        var result = CampEngine.ValidateUpgrade(camp, CampCatalog.TentId);

        Assert.True(result.Success);
        Assert.Equal(1, result.NewLevel);
        Assert.Equal(CampCatalog.TentId, result.ModuleId);
    }

    // --- Service layer: persistence round-trips and the shared coin account ---

    private static BaseCampService Service(InMemoryCampStore campStore, ActivityStats stats, out CoinAccountService account)
    {
        var statsSource = new FakeActivityStatsSource(stats);
        account = new CoinAccountService(new InMemoryCityStore(), new InMemoryTowerDefenseStore(), campStore, statsSource);
        return new BaseCampService(campStore, statsSource, account);
    }

    /// <summary>
    /// Camp whose ledger was anchored a few hours ago, so exactly
    /// <paramref name="hours"/> × <see cref="CampEconomy.BaseRatePerHour"/> supplies are waiting.
    /// No trips are recorded, which keeps the rate at its base value.
    /// </summary>
    private static async Task<InMemoryCampStore> CampWithAccruedHoursAsync(int hours)
    {
        var campStore = new InMemoryCampStore();
        await campStore.SaveLogAsync(new CampLog
        {
            ID = Guid.NewGuid(),
            LastCollectedAt = DateTimeOffset.UtcNow.AddHours(-hours),
            BankedSupplies = CampEconomy.StartingSupplies,
        });

        return campStore;
    }

    [Fact]
    public async Task FirstVisit_AnchorsTheCampSoItStartsProducing()
    {
        var campStore = new InMemoryCampStore();
        var service = Service(campStore, StatsWith(), out _);

        var camp = await service.GetCampAsync();

        // Without a ledger row the anchor would move with every read and the camp would never
        // fill up, so the first look at the camp opens it.
        Assert.Equal(CampEconomy.StartingSupplies, camp.Supplies);
        Assert.Equal(0, camp.Stock);
    }

    [Fact]
    public async Task Collect_BanksTheStockAndMovesTheAnchor()
    {
        var campStore = await CampWithAccruedHoursAsync(3);
        var service = Service(campStore, StatsWith(), out _);

        var result = await service.CollectAsync();

        Assert.True(result.Success);
        Assert.Equal(3 * CampEconomy.BaseRatePerHour, result.Collected);

        var after = await service.GetCampAsync();
        Assert.Equal(0, after.Stock);
        Assert.Equal(CampEconomy.StartingSupplies + result.Collected, after.Supplies);
        Assert.Equal(result.Collected, after.TotalCollected);
    }

    [Fact]
    public async Task Collect_OnAnEmptyCamp_FailsAndMintsNothing()
    {
        var campStore = new InMemoryCampStore();
        var service = Service(campStore, StatsWith(), out _);

        var first = await service.CollectAsync();
        var second = await service.CollectAsync();

        // The welcome stock is granted once and is not lying in the camp, so there is nothing
        // to bank and tapping cannot turn into supplies.
        Assert.False(first.Success);
        Assert.False(second.Success);
        Assert.Contains("leer", second.ErrorMessage);
        Assert.Equal(CampEconomy.StartingSupplies, (await service.GetCampAsync()).Supplies);
    }

    [Fact]
    public async Task Collect_LeavesTheCoinBalanceUntouched()
    {
        var campStore = await CampWithAccruedHoursAsync(3);
        var service = Service(campStore, StatsWith(), out var account);

        int before = (await account.GetAsync()).Spent;
        await service.CollectAsync();
        int after = (await account.GetAsync()).Spent;

        // Supplies are camp-private: harvesting must neither mint nor burn coins.
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Upgrade_PersistsOneRowPerModuleAndChargesItsPrice()
    {
        var campStore = await CampWithAccruedHoursAsync(6);
        var service = Service(campStore, StatsWith(), out var account);

        await service.CollectAsync();
        var result = await service.TryUpgradeAsync(CampCatalog.TentId);
        var second = await service.TryUpgradeAsync(CampCatalog.KitchenId);
        var camp = await service.GetCampAsync();

        Assert.True(result.Success);
        Assert.True(second.Success);
        Assert.Equal(2, campStore.ModuleRowCount);
        Assert.Equal(1, camp.LevelOf(CampCatalog.TentId));
        Assert.Equal(1, camp.LevelOf(CampCatalog.KitchenId));

        // 250 welcome + 36 harvested − 150 tent − 120 kitchen: the prices really are paid.
        Assert.Equal(CampEconomy.StartingSupplies + 36 - 150 - 120, camp.Supplies);

        // Tent and kitchen are supply-only, so the shared coin balance is unaffected.
        Assert.Equal((await account.GetAsync()).Earned, camp.Coins);
    }

    [Fact]
    public async Task Upgrade_IsNotFreeOnceTheCampHasBeenHarvested()
    {
        var campStore = await CampWithAccruedHoursAsync(6);
        var service = Service(campStore, StatsWith(), out _);

        await service.CollectAsync();
        var before = await service.GetCampAsync();

        await service.TryUpgradeAsync(CampCatalog.TentId);
        var after = await service.GetCampAsync();

        // The stock is derived from the anchor, so a purchase must not be able to hide behind
        // it: the 150 supplies of the first tent level leave the balance.
        Assert.Equal(150, CampCatalog.Find(CampCatalog.TentId)!.CostFor(1).Supplies);
        Assert.Equal(before.Supplies - 150, after.Supplies);
        Assert.Equal(0, after.Stock);
    }

    [Fact]
    public async Task Upgrade_KeepsTheUnharvestedStockWaiting()
    {
        var campStore = await CampWithAccruedHoursAsync(3);
        var service = Service(campStore, StatsWith(), out _);

        await service.TryUpgradeAsync(CampCatalog.TentId);
        var camp = await service.GetCampAsync();

        // Buying does not move the anchor, so the 18 supplies stay in the camp and can still
        // be harvested afterwards.
        Assert.Equal(3 * CampEconomy.BaseRatePerHour, camp.Stock);
        Assert.Equal(CampEconomy.StartingSupplies - 150, camp.Supplies);
    }

    [Fact]
    public async Task Upgrade_IsRejectedWhenTheSuppliesAreNotThereYet()
    {
        var campStore = new InMemoryCampStore();
        var service = Service(campStore, StatsWith(), out _);

        // 250 welcome supplies pay for the 150-supply first tent level, leaving 100 — not
        // enough for the 400-supply second level.
        await service.TryUpgradeAsync(CampCatalog.TentId);
        var result = await service.TryUpgradeAsync(CampCatalog.TentId);

        Assert.False(result.Success);
        Assert.Contains("Vorräte", result.ErrorMessage);
        Assert.Equal(1, campStore.ModuleRowCount);
        Assert.Equal(CampEconomy.StartingSupplies - 150, (await service.GetCampAsync()).Supplies);
    }

    [Fact]
    public async Task Upgrade_IsRejectedForAnUnknownModule()
    {
        var campStore = new InMemoryCampStore();
        var service = Service(campStore, StatsWith(), out _);

        var result = await service.TryUpgradeAsync("greenhouse");

        Assert.False(result.Success);
        Assert.Equal(0, campStore.ModuleRowCount);
    }
}
