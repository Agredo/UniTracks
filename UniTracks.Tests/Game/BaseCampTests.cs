using UniTracks.Games.BaseCamp;
using UniTracks.Games.BaseCamp.Persistence;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Tests.Game;

/// <summary>
/// The base camp is an idle game, but its supplies must never grow on pure waiting:
/// production is driven by the player's real trips, decays during pauses and is capped by
/// the camp's capacity. Nothing but module levels and the harvest anchor is persisted.
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

        Assert.Equal(camp.Capacity, camp.Supplies);
        Assert.True(camp.IsFull);
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
    public void Harvest_ResetsTheStockToTheWelcomeAmount()
    {
        var stats = StatsWith(Run(40, Now.AddHours(-50)));
        var log = new CampLog { ID = Guid.NewGuid(), LastCollectedAt = Now, TotalCollected = 400 };

        var camp = CampEngine.Rebuild(Array.Empty<CampModule>(), log, stats, Now);

        // The anchor is now, so nothing has accrued since the last harvest.
        Assert.Equal(CampEconomy.StartingSupplies, camp.Supplies);
        Assert.Equal(Now, camp.LastCollectedAt);
        Assert.Equal(400, camp.TotalCollected);
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
}
