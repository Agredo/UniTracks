using UniTracks.Games.Shared.Economy;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Services.Game;
using UniTracks.Tests.TowerDefense.Fakes;

namespace UniTracks.Tests.TowerDefense;

/// <summary>
/// Service-level rules around a run: what a snapshot stores while a wave is running, what may be
/// resumed afterwards, and the coin-gated unlock/energy flows.
/// </summary>
public sealed class TowerDefenseServiceTests
{
    private readonly InMemoryTowerDefenseStore store = new();
    private readonly FakeGameCatalogService catalog = new();

    private TowerDefenseService CreateService() => new(store, catalog);

    // ---------------------------------------------------------------- resume gate (#6)

    [Fact]
    public async Task LoadRunAsync_ReturnsNull_WhenThereIsNoSnapshot()
    {
        Assert.Null(await CreateService().LoadRunAsync());
    }

    [Fact]
    public async Task LoadRunAsync_RefusesARunWhoseLivesAreGone()
    {
        store.SeedRun(new DefenseRunProgress
        {
            ID = Guid.NewGuid(),
            Wave = 7,
            Energy = 500,
            Lives = 0,
            Score = 900,
            TowersJson = "[{\"X\":1,\"Y\":1,\"TowerId\":\"spray\"}]",
        });

        // A finished run must not be resumable: restoring it would hand back the full lives and
        // the towers of the failed attempt, which could be farmed into free towers at the same wave.
        Assert.Null(await CreateService().LoadRunAsync());
    }

    [Fact]
    public async Task LoadRunAsync_KeepsAnEmptyEnergyBudgetAtZero()
    {
        store.SeedRun(new DefenseRunProgress
        {
            ID = Guid.NewGuid(),
            Wave = 3,
            Energy = 0,
            Lives = 20,
            Score = 40,
        });

        var state = await CreateService().LoadRunAsync();

        // The stored value is authoritative. A fallback to "starting energy" for a stored 0
        // refunded the whole credit to a player who had simply spent everything.
        Assert.NotNull(state);
        Assert.Equal(0, state!.Energy);
    }

    [Fact]
    public async Task LoadRunAsync_RestoresTheSnapshotAtAWaveBoundary()
    {
        store.SeedRun(new DefenseRunProgress
        {
            ID = Guid.NewGuid(),
            Wave = 4,
            MapId = "waldwiese",
            Energy = 123,
            Lives = 19,
            Score = 250,
            TowersJson = "[{\"X\":1,\"Y\":2,\"TowerId\":\"spray\"},{\"X\":3,\"Y\":4,\"TowerId\":\"candle\"}]",
        });

        var state = await CreateService().LoadRunAsync();

        Assert.NotNull(state);
        Assert.Equal(123, state!.Energy);
        Assert.Equal(19, state.Lives);
        Assert.Equal(250, state.Score);
        Assert.Equal(4, state.NextWave);
        Assert.Equal(DefensePhase.Building, state.Phase);
        Assert.Equal(MapCatalog.Default.Id, state.Map.Id);
        Assert.Equal(2, state.Towers.Count);
        Assert.Contains(state.Towers, t => t is { X: 1, Y: 2, TowerId: "spray" });
        Assert.Contains(state.Towers, t => t is { X: 3, Y: 4, TowerId: "candle" });
    }

    [Fact]
    public async Task LoadRunAsync_SurvivesABrokenTowerSnapshot()
    {
        store.SeedRun(new DefenseRunProgress
        {
            ID = Guid.NewGuid(),
            Wave = 2,
            Energy = 10,
            Lives = 10,
            TowersJson = "[{\"X\":1,\"Y\":1,\"TowerId\":\"\"}]",
        });

        var state = await CreateService().LoadRunAsync();

        Assert.NotNull(state);
        Assert.Empty(state!.Towers);
    }

    [Fact]
    public async Task LoadRunAsync_AdoptsTheCurrentUnlocksAndClearBonus()
    {
        catalog.Stats = new ActivityStats { Xp = 300 }; // level 4 → 20 + 3×10
        store.SeedRun(new DefenseRunProgress { ID = Guid.NewGuid(), Wave = 2, Energy = 5, Lives = 5 });

        var state = await CreateService().LoadRunAsync();

        Assert.NotNull(state);
        Assert.Equal(EnergyEconomy.ComputeClearBonus(catalog.Stats), state!.ClearBonus);
        Assert.Empty(state.UnlockedTowerIds);
    }

    // ---------------------------------------------------------------- score snapshot (#8)

    [Fact]
    public async Task SaveRunAsync_WhileAWaveRuns_PersistsTheScoreTheWaveStartedWith()
    {
        var service = CreateService();
        var state = DefenseEngine.NewRun(Array.Empty<string>(), MapCatalog.Default, 60, 20);
        DefenseEngine.StartWave(state);

        // Kills of the aborted attempt: the wave is replayed from its start on resume, so these
        // points are earned again and must not be banked now.
        state.Score = 400;

        await service.SaveRunAsync(state);

        Assert.Equal(state.WaveStartScore, store.Run!.Score);
        Assert.Equal(DefensePhase.WaveRunning, state.Phase);
    }

    [Fact]
    public async Task SaveRunAsync_BetweenWaves_PersistsTheLiveScore()
    {
        var service = CreateService();
        var state = DefenseEngine.NewRun(Array.Empty<string>(), MapCatalog.Default, 60, 20);
        state.Score = 275;

        await service.SaveRunAsync(state);

        Assert.Equal(275, store.Run!.Score);
    }

    [Fact]
    public async Task SaveRunAsync_RoundTripsWaveEnergyLivesAndLayout()
    {
        var service = CreateService();
        var state = DefenseEngine.NewRun(new[] { "spray" }, MapCatalog.Default, 60, 20);
        DefenseEngine.PlaceTower(state, "spray", 4, 6);
        DefenseEngine.StartWave(state);
        TickUntilWaveEnds(state);
        state.Lives -= 3;
        state.Energy += 17;

        await service.SaveRunAsync(state);
        var restored = await service.LoadRunAsync();

        Assert.NotNull(restored);
        Assert.Equal(state.NextWave, restored!.NextWave);
        Assert.Equal(state.Energy, restored.Energy);
        Assert.Equal(state.Lives, restored.Lives);
        Assert.Equal(state.Score, restored.Score);
        Assert.Equal(state.Towers.Count, restored.Towers.Count);
        Assert.Equal((4, 6, "spray"), (restored.Towers[0].X, restored.Towers[0].Y, restored.Towers[0].TowerId));
    }

    [Fact]
    public async Task SaveRunAsync_UpdatesTheSingleRowInsteadOfInsertingASecond()
    {
        var service = CreateService();
        var state = DefenseEngine.NewRun(Array.Empty<string>(), MapCatalog.Default, 60, 20);

        await service.SaveRunAsync(state);
        await service.SaveRunAsync(state);
        await service.SaveRunAsync(state);

        Assert.Equal(3, store.SaveRunCalls);
        Assert.Empty(store.Unlocks);
        Assert.NotNull(store.Run);
    }

    [Fact]
    public async Task ClearRunAsync_RemovesTheSnapshot()
    {
        var service = CreateService();
        await service.SaveRunAsync(DefenseEngine.NewRun(Array.Empty<string>(), MapCatalog.Default, 60, 20));

        await service.ClearRunAsync();

        Assert.Null(store.Run);
        Assert.Null(await service.LoadRunAsync());
    }

    // ---------------------------------------------------------------- records

    [Fact]
    public async Task SaveRunResultAsync_StoresTheFirstRecord()
    {
        var profile = await CreateService().SaveRunResultAsync(clearedWave: 5, score: 320);

        Assert.Equal(5, profile.BestWave);
        Assert.Equal(320, profile.BestScore);
        Assert.NotNull(store.Record);
    }

    [Fact]
    public async Task SaveRunResultAsync_KeepsTheOldRecordWhenNothingImproved()
    {
        var service = CreateService();
        await service.SaveRunResultAsync(clearedWave: 5, score: 320);

        var profile = await service.SaveRunResultAsync(clearedWave: 4, score: 999);

        Assert.Equal(5, profile.BestWave);
        Assert.Equal(320, profile.BestScore);
    }

    [Fact]
    public async Task SaveRunResultAsync_AcceptsAHigherScoreAtTheSameWave()
    {
        var service = CreateService();
        await service.SaveRunResultAsync(clearedWave: 5, score: 320);

        var profile = await service.SaveRunResultAsync(clearedWave: 5, score: 500);

        Assert.Equal(5, profile.BestWave);
        Assert.Equal(500, profile.BestScore);
    }

    [Fact]
    public async Task SaveRunResultAsync_WithZeroWaves_DoesNotOverwriteAnExistingRecord()
    {
        var service = CreateService();
        await service.SaveRunResultAsync(clearedWave: 3, score: 100);

        var profile = await service.SaveRunResultAsync(clearedWave: 0, score: 0);

        Assert.Equal(3, profile.BestWave);
        Assert.Equal(100, profile.BestScore);
    }

    // ---------------------------------------------------------------- unlocks

    [Fact]
    public async Task TryUnlockAsync_RejectsAnUnknownTower()
    {
        var result = await CreateService().TryUnlockAsync("gibt-es-nicht");

        Assert.False(result.Success);
        Assert.Empty(store.Unlocks);
    }

    [Fact]
    public async Task TryUnlockAsync_AcceptsTheFreeStarterWithoutWritingAnything()
    {
        var result = await CreateService().TryUnlockAsync("spray");

        Assert.True(result.Success);
        Assert.Empty(store.Unlocks);
    }

    [Fact]
    public async Task TryUnlockAsync_RejectsATowerAboveTheCurrentLevel()
    {
        catalog.Coins = 10_000;
        catalog.Stats = new ActivityStats { Xp = 0 }; // level 1

        var result = await CreateService().TryUnlockAsync("candle");

        Assert.False(result.Success);
        Assert.Contains("Level 2", result.ErrorMessage);
        Assert.Empty(store.Unlocks);
    }

    [Fact]
    public async Task TryUnlockAsync_RejectsAnUnaffordableTower()
    {
        catalog.Coins = TowerCatalog.Find("candle")!.UnlockCost - 1;
        catalog.Stats = new ActivityStats { Xp = 100 }; // level 2

        var result = await CreateService().TryUnlockAsync("candle");

        Assert.False(result.Success);
        Assert.Contains("Münzen", result.ErrorMessage);
        Assert.Empty(store.Unlocks);
    }

    [Fact]
    public async Task TryUnlockAsync_UnlocksAnAffordableTower()
    {
        catalog.Coins = 1000;
        catalog.Stats = new ActivityStats { Xp = 100 };

        var result = await CreateService().TryUnlockAsync("candle");

        Assert.True(result.Success);
        var unlock = Assert.Single(store.Unlocks);
        Assert.Equal("candle", unlock.TowerId);
        Assert.NotEqual(Guid.Empty, unlock.ID);
    }

    [Fact]
    public async Task TryUnlockAsync_ReportsAnAlreadyUnlockedTower()
    {
        catalog.Coins = 1000;
        catalog.Stats = new ActivityStats { Xp = 100 };
        var service = CreateService();
        await service.TryUnlockAsync("candle");

        var result = await service.TryUnlockAsync("candle");

        Assert.False(result.Success);
        Assert.Contains("bereits", result.ErrorMessage);
        Assert.Single(store.Unlocks);
    }

    [Fact]
    public async Task TryUnlockAsync_BlocksAPrestigeTowerWithoutItsAchievement()
    {
        catalog.Coins = 100_000;
        catalog.Stats = new ActivityStats { Xp = 400 }; // level 5, but no achievement

        var result = await CreateService().TryUnlockAsync("gecko");

        Assert.False(result.Success);
        Assert.Contains("100 km gesamt", result.ErrorMessage);
        Assert.Empty(store.Unlocks);
    }

    [Fact]
    public async Task TryUnlockAsync_AllowsAPrestigeTowerWithItsAchievement()
    {
        catalog.Coins = 100_000;
        catalog.Stats = new ActivityStats
        {
            Xp = 400,
            UnlockedAchievements = 1,
            UnlockedAchievementIds = new[] { "hundred-km" },
        };

        var result = await CreateService().TryUnlockAsync("gecko");

        Assert.True(result.Success);
        Assert.Equal("gecko", Assert.Single(store.Unlocks).TowerId);
    }

    // ---------------------------------------------------------------- coin-funded energy

    [Theory]
    [InlineData(0)]
    [InlineData(-25)]
    public async Task BuyEnergyAsync_RejectsNonPositiveAmounts(int energy)
    {
        catalog.Coins = 10_000;

        var result = await CreateService().BuyEnergyAsync(NewRun(), energy);

        Assert.False(result.Success);
        Assert.Empty(store.Purchases);
    }

    [Fact]
    public async Task BuyEnergyAsync_RejectsAnUnaffordableTopUp()
    {
        int cost = EnergyEconomy.CoinCostForEnergy(EnergyEconomy.EnergyPackSize);
        catalog.Coins = cost - 1;
        var state = NewRun();

        var result = await CreateService().BuyEnergyAsync(state, EnergyEconomy.EnergyPackSize);

        Assert.False(result.Success);
        Assert.Contains("Münzen", result.ErrorMessage);
        Assert.Equal(EnergyEconomy.StartingEnergy, state.Energy);
        Assert.Empty(store.Purchases);
    }

    [Fact]
    public async Task BuyEnergyAsync_GrantsEnergyAndRecordsTheSpend()
    {
        catalog.Coins = 10_000;
        var state = NewRun();

        var result = await CreateService().BuyEnergyAsync(state, EnergyEconomy.EnergyPackSize);

        Assert.True(result.Success);
        Assert.Equal(EnergyEconomy.StartingEnergy + EnergyEconomy.EnergyPackSize, state.Energy);
        var purchase = Assert.Single(store.Purchases);
        Assert.Equal(EnergyEconomy.EnergyPackSize, purchase.Energy);
        Assert.Equal(EnergyEconomy.CoinCostForEnergy(EnergyEconomy.EnergyPackSize), purchase.Coins);
    }

    [Fact]
    public async Task BuyEnergyAsync_PersistsTheGrantedEnergyWithTheRun()
    {
        catalog.Coins = 10_000;
        var state = NewRun();

        await CreateService().BuyEnergyAsync(state, EnergyEconomy.EnergyPackSize);

        Assert.NotNull(store.Run);
        Assert.Equal(EnergyEconomy.StartingEnergy + EnergyEconomy.EnergyPackSize, store.Run!.Energy);
    }

    [Fact]
    public async Task BuyEnergyAsync_DuringAWave_StillPersistsTheWaveStartScore()
    {
        catalog.Coins = 10_000;
        var state = NewRun();
        DefenseEngine.StartWave(state);

        await CreateService().BuyEnergyAsync(state, EnergyEconomy.EnergyPackSize);

        // The top-up itself is allowed at any time; the point of this test is that the snapshot
        // taken right after still carries the wave-start score, not the aborted attempt's kills.
        Assert.Equal(0, store.Run!.Score);
    }

    // ---------------------------------------------------------------- profile

    [Fact]
    public async Task GetProfileAsync_ReportsCoinsLevelAndStoredRecord()
    {
        catalog.Coins = 777;
        catalog.Stats = new ActivityStats { Xp = 250, UnlockedAchievements = 2 };
        var service = CreateService();
        await service.TryUnlockAsync("spray");
        store.SeedRun(new DefenseRunProgress { ID = Guid.NewGuid(), Wave = 2, Energy = 5, Lives = 5 });
        await service.SaveRunResultAsync(clearedWave: 6, score: 640);

        var profile = await service.GetProfileAsync();

        Assert.Equal(777, profile.Coins);
        Assert.Equal(3, profile.Level); // 250 / 100 + 1
        Assert.Equal(6, profile.BestWave);
        Assert.Equal(640, profile.BestScore);
        Assert.Equal(EnergyEconomy.ComputeStartingEnergy(catalog.Stats), profile.StartingEnergy);
        Assert.Equal(EnergyEconomy.ComputeClearBonus(catalog.Stats), profile.ClearBonus);
    }

    [Fact]
    public async Task GetProfileAsync_ListsTheUnlockedTowerIds()
    {
        catalog.Coins = 1000;
        catalog.Stats = new ActivityStats { Xp = 100 };
        var service = CreateService();
        await service.TryUnlockAsync("candle");

        var profile = await service.GetProfileAsync();

        Assert.Equal(new[] { "candle" }, profile.UnlockedTowerIds);
    }

    private static DefenseState NewRun() =>
        DefenseEngine.NewRun(Array.Empty<string>(), MapCatalog.Default, EnergyEconomy.StartingEnergy, EnergyEconomy.BaseClearBonus);

    private static void TickUntilWaveEnds(DefenseState state, int maxMs = 180_000)
    {
        int elapsed = 0;
        while (state.Phase == DefensePhase.WaveRunning && elapsed < maxMs)
        {
            DefenseEngine.Tick(state, 50);
            elapsed += 50;
        }
    }
}
