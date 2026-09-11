using UniTracks.Games.Shared.Economy;
using UniTracks.Games.TowerDefense;
using UniTracks.Games.TowerDefense.Persistence;

namespace UniTracks.Tests.TowerDefense;

/// <summary>
/// Covers the pure simulation in <see cref="DefenseEngine"/>: placement validation, the energy
/// economy, the wave state machine and the loss/clear transitions. No persistence involved.
/// </summary>
public sealed class DefenseEngineTests
{
    private const double TickMs = 50;

    [Fact]
    public void NewRun_TakesLivesFromTheMap_AndEnergyFromTheCaller()
    {
        var state = DefenseEngine.NewRun(new[] { "spray" }, MapCatalog.Default, 60, 20);

        Assert.Equal(MapCatalog.Default.StartLives, state.Lives);
        Assert.Equal(60, state.Energy);
        Assert.Equal(20, state.ClearBonus);
        Assert.Equal(DefensePhase.Building, state.Phase);
        Assert.Equal(1, state.NextWave);
        Assert.Equal(new[] { "spray" }, state.UnlockedTowerIds);
    }

    [Theory]
    [InlineData("does-not-exist", DefenseError.UnknownTower)]
    [InlineData("candle", DefenseError.TowerLocked)]
    public void ValidatePlacement_RejectsUnknownAndLockedTowers(string towerId, DefenseError expected)
    {
        var state = NewRun(energy: 10_000);

        var result = DefenseEngine.ValidatePlacement(state, towerId, 0, 0);

        Assert.False(result.Success);
        Assert.Equal(expected, result.Error);
    }

    [Fact]
    public void ValidatePlacement_RejectsCoordinatesOutsideTheGrid()
    {
        var state = NewRun(energy: 10_000);

        Assert.Equal(DefenseError.OutOfBounds, DefenseEngine.ValidatePlacement(state, "spray", -1, 0).Error);
        Assert.Equal(DefenseError.OutOfBounds, DefenseEngine.ValidatePlacement(state, "spray", 0, -1).Error);
        Assert.Equal(
            DefenseError.OutOfBounds,
            DefenseEngine.ValidatePlacement(state, "spray", MapCatalog.Default.GridWidth, 0).Error);
        Assert.Equal(
            DefenseError.OutOfBounds,
            DefenseEngine.ValidatePlacement(state, "spray", 0, MapCatalog.Default.GridHeight).Error);
    }

    [Fact]
    public void ValidatePlacement_RejectsTrailWaterAndForestTiles()
    {
        var state = NewRun(energy: 10_000);

        // (2, 0) lies on the Waldwiese trail, (7, 11) is forest.
        Assert.Equal(DefenseError.NotBuildable, DefenseEngine.ValidatePlacement(state, "spray", 2, 0).Error);
        Assert.Equal(DefenseError.NotBuildable, DefenseEngine.ValidatePlacement(state, "spray", 7, 11).Error);

        var parkState = NewRun(energy: 10_000, map: MapCatalog.Find("park-promenade"));

        // (0, 10) is the pond on the Park-Promenade.
        Assert.Equal(DefenseError.NotBuildable, DefenseEngine.ValidatePlacement(parkState, "spray", 0, 10).Error);
    }

    [Fact]
    public void ValidatePlacement_RejectsAnOccupiedTile()
    {
        var state = NewRun(energy: 10_000);
        Assert.True(DefenseEngine.PlaceTower(state, "spray", 0, 0).Success);

        var result = DefenseEngine.ValidatePlacement(state, "spray", 0, 0);

        Assert.Equal(DefenseError.TileOccupied, result.Error);
    }

    [Fact]
    public void ValidatePlacement_RejectsPlacementWithoutEnoughEnergy()
    {
        var state = NewRun(energy: TowerCatalog.Find("spray")!.EnergyCost - 1);

        var result = DefenseEngine.ValidatePlacement(state, "spray", 0, 0);

        Assert.Equal(DefenseError.NotEnoughEnergy, result.Error);
    }

    [Fact]
    public void ValidatePlacement_DoesNotMutateAnything()
    {
        var state = NewRun(energy: 1000);

        DefenseEngine.ValidatePlacement(state, "spray", 0, 0);

        Assert.Empty(state.Towers);
        Assert.Equal(1000, state.Energy);
    }

    [Fact]
    public void PlaceTower_SpendsTheEnergyCost_AndRecordsTheTower()
    {
        var state = NewRun(energy: 100);
        var spray = TowerCatalog.Find("spray")!;

        var result = DefenseEngine.PlaceTower(state, "spray", 0, 0);

        Assert.True(result.Success);
        Assert.Equal(-spray.EnergyCost, result.EnergyDelta);
        Assert.Equal(100 - spray.EnergyCost, state.Energy);
        var tower = Assert.Single(state.Towers);
        Assert.Equal(0, tower.X);
        Assert.Equal(0, tower.Y);
        Assert.Equal("spray", tower.TowerId);
    }

    [Fact]
    public void PlaceTower_FillsExactlyTheBuildableTilesOfTheMap()
    {
        var state = NewRun(energy: 1_000_000);

        for (int y = 0; y < state.Map.GridHeight; y++)
        {
            for (int x = 0; x < state.Map.GridWidth; x++)
            {
                if (state.IsBuildable(x, y))
                {
                    Assert.True(
                        DefenseEngine.PlaceTower(state, "spray", x, y).Success,
                        $"({x},{y}) should be buildable");
                }
            }
        }

        Assert.Equal(state.Map.BuildableCount, state.Towers.Count);
    }

    [Fact]
    public void SellTower_RefundsHalfOfTheEnergyCost()
    {
        var state = NewRun(energy: 100);
        var spray = TowerCatalog.Find("spray")!;
        DefenseEngine.PlaceTower(state, "spray", 0, 0);
        int energyBeforeSale = state.Energy;

        var result = DefenseEngine.SellTower(state, 0, 0);

        Assert.True(result.Success);
        Assert.Equal((int)Math.Floor(spray.EnergyCost * DefenseEngine.SellRefundFraction), result.EnergyDelta);
        Assert.Equal(energyBeforeSale + result.EnergyDelta, state.Energy);
        Assert.Empty(state.Towers);
    }

    [Fact]
    public void SellTower_OnAnEmptyTileFails()
    {
        var state = NewRun(energy: 100);

        var result = DefenseEngine.SellTower(state, 0, 0);

        Assert.Equal(DefenseError.TileEmpty, result.Error);
        Assert.Equal(100, state.Energy);
    }

    [Fact]
    public void StartWave_SnapshotsTheCurrentScoreIntoWaveStartScore()
    {
        var state = NewRun();
        state.Score = 123;

        Assert.True(DefenseEngine.StartWave(state).Success);

        // A wave that is interrupted is replayed from its beginning, so the score it started with
        // is the only value that may be persisted while it runs (see TowerDefenseService.SaveRunAsync).
        Assert.Equal(123, state.WaveStartScore);
        Assert.Equal(DefensePhase.WaveRunning, state.Phase);
    }

    [Fact]
    public void StartWave_QueuesTheComposedWave_AndResetsTheCounters()
    {
        var state = NewRun();
        state.NextWave = 3;
        state.WaveSpawned = 99;
        state.WaveLeaked = 99;
        state.SpawnCooldownMs = 500;

        Assert.True(DefenseEngine.StartWave(state).Success);

        Assert.Equal(WaveCatalog.For(3, state.Map).Count, state.PendingSpawns.Count);
        Assert.Equal(0, state.WaveSpawned);
        Assert.Equal(0, state.WaveLeaked);
        Assert.Equal(0, state.SpawnCooldownMs);
    }

    [Fact]
    public void StartWave_WhileAWaveIsRunningFails()
    {
        var state = NewRun();
        DefenseEngine.StartWave(state);

        var result = DefenseEngine.StartWave(state);

        Assert.Equal(DefenseError.WaveRunning, result.Error);
    }

    [Fact]
    public void Tick_DoesNothing_OutsideOfARunningWave()
    {
        var state = NewRun();
        state.Lives = 5;
        state.Score = 7;

        DefenseEngine.Tick(state, 5000);

        Assert.Equal(DefensePhase.Building, state.Phase);
        Assert.Equal(5, state.Lives);
        Assert.Equal(7, state.Score);
        Assert.Equal(1, state.NextWave);
    }

    [Fact]
    public void Tick_LetsEnemiesLeak_AndStillClearsTheWave()
    {
        var state = NewRun(energy: 60, clearBonus: 20);
        DefenseEngine.StartWave(state);
        int waveEnemies = state.PendingSpawns.Count;

        TickUntilWaveEnds(state);

        Assert.Equal(DefensePhase.Building, state.Phase);
        Assert.Equal(2, state.NextWave);
        Assert.Equal(waveEnemies, state.WaveLeaked);
        Assert.Equal(MapCatalog.Default.StartLives - waveEnemies * 2, state.Lives);

        // Nothing was defeated, so the merit-based clear bonus pays out nothing at all.
        Assert.Equal(0, state.Score);
        Assert.Equal(60, state.Energy);

        // A leaky clear never counts toward the record.
        Assert.Equal(0, state.BestClearWave);
        Assert.Equal(0, state.BestClearScore);
    }

    [Fact]
    public void Tick_ClearingAWaveWithoutLeaks_SetsTheRecordAndPaysTheFullBonus()
    {
        var state = NewRun(new[] { "zapper" }, energy: 20_000, clearBonus: 20);

        // A zapper on every buildable tile — nothing survives the walk to the exit.
        for (int y = 0; y < state.Map.GridHeight; y++)
        {
            for (int x = 0; x < state.Map.GridWidth; x++)
            {
                if (state.IsBuildable(x, y))
                {
                    DefenseEngine.PlaceTower(state, "zapper", x, y);
                }
            }
        }

        int placed = state.Towers.Count;
        int zapperCost = TowerCatalog.Find("zapper")!.EnergyCost;
        DefenseEngine.StartWave(state);

        TickUntilWaveEnds(state);

        Assert.Equal(DefensePhase.Building, state.Phase);
        Assert.Equal(0, state.WaveLeaked);
        Assert.Equal(MapCatalog.Default.StartLives, state.Lives);
        Assert.Equal(4 * 5, state.Score); // four mosquitoes, 5 points each
        Assert.Equal(20_000 - placed * zapperCost + 20, state.Energy);
        Assert.Equal(1, state.BestClearWave);
        Assert.Equal(state.Score, state.BestClearScore);
        Assert.Equal(2, state.NextWave);
    }

    [Fact]
    public void Tick_PaysTheClearBonusScaledByTheDefeatedShareOfTheWave()
    {
        var state = NewRun(energy: 0, clearBonus: 100);
        state.Phase = DefensePhase.WaveRunning;
        state.WaveSpawned = 4;
        state.WaveLeaked = 1;

        DefenseEngine.Tick(state, 16);

        Assert.Equal(75, state.Energy); // round(100 × 3/4)
        Assert.Equal(DefensePhase.Building, state.Phase);
    }

    [Fact]
    public void Tick_PaysNothing_WhenTheWaveSpawnedNoEnemiesAtAll()
    {
        var state = NewRun(energy: 0, clearBonus: 100);
        state.Phase = DefensePhase.WaveRunning;

        DefenseEngine.Tick(state, 16);

        Assert.Equal(0, state.Energy);
        Assert.Equal(DefensePhase.Building, state.Phase);
    }

    [Fact]
    public void Tick_LosingTheLastLifeEndsTheRun()
    {
        var state = NewRun(energy: 0, clearBonus: 100);
        state.Lives = 1;
        DefenseEngine.StartWave(state);

        TickUntilWaveEnds(state);

        Assert.Equal(DefensePhase.Lost, state.Phase);
        Assert.Equal(0, state.Lives);
        Assert.Empty(state.Enemies);
        Assert.Empty(state.Projectiles);
        Assert.Empty(state.PendingSpawns);

        // A lost run neither advances the wave counter nor pays a clear bonus.
        Assert.Equal(1, state.NextWave);
        Assert.Equal(0, state.Energy);
        Assert.Equal(0, state.BestClearWave);
    }

    [Fact]
    public void Tick_AfterTheRunIsLost_IsANoOp()
    {
        var state = NewRun(energy: 0);
        state.Lives = 1;
        DefenseEngine.StartWave(state);
        TickUntilWaveEnds(state);
        Assert.Equal(DefensePhase.Lost, state.Phase);

        DefenseEngine.Tick(state, 10_000);

        Assert.Equal(DefensePhase.Lost, state.Phase);
        Assert.Equal(0, state.Lives);
        Assert.Equal(1, state.NextWave);
    }

    [Theory]
    [InlineData(DefensePhase.Building, 4, 3)]
    [InlineData(DefensePhase.WaveRunning, 4, 4)]
    [InlineData(DefensePhase.Lost, 4, 4)]
    public void ClearedWave_ReportsTheLastFullyClearedWave(DefensePhase phase, int nextWave, int expected)
    {
        var state = NewRun();
        state.Phase = phase;
        state.NextWave = nextWave;

        Assert.Equal(expected, state.ClearedWave);
    }

    [Fact]
    public void ComputeUnlockSpent_SumsTheCatalogPrices_AndIgnoresUnknownIds()
    {
        var unlocks = new[]
        {
            Unlock("spray"),    // 0
            Unlock("candle"),   // 150
            Unlock("zapper"),   // 400
            Unlock("retired-tower"),
        };

        Assert.Equal(550, DefenseEngine.ComputeUnlockSpent(unlocks));
    }

    [Fact]
    public void ComputeEnergySpent_SumsTheCoinsOfEveryPurchase()
    {
        var purchases = new[]
        {
            new EnergyPurchase { ID = Guid.NewGuid(), Energy = 25, Coins = 50 },
            new EnergyPurchase { ID = Guid.NewGuid(), Energy = 50, Coins = 100 },
        };

        Assert.Equal(150, DefenseEngine.ComputeEnergySpent(purchases));
    }

    [Fact]
    public void GrantEnergy_AddsToTheRunningBudget()
    {
        var state = NewRun(energy: 10);

        DefenseEngine.GrantEnergy(state, EnergyEconomy.EnergyPackSize);

        Assert.Equal(10 + EnergyEconomy.EnergyPackSize, state.Energy);
    }

    [Fact]
    public void AFullRun_ScalesWavesUntilTheLivesRunOut()
    {
        var state = NewRun(energy: 0);
        int guard = 0;

        while (state.Phase != DefensePhase.Lost && guard++ < 50)
        {
            DefenseEngine.StartWave(state);
            TickUntilWaveEnds(state);
        }

        Assert.Equal(DefensePhase.Lost, state.Phase);
        Assert.Equal(0, state.Lives);
        Assert.True(state.NextWave > 1, "the run must have cleared at least one wave before dying");
        Assert.Equal(0, state.Energy);
    }

    private static DefenseState NewRun(
        IEnumerable<string>? unlocked = null,
        int energy = 1000,
        int clearBonus = 20,
        DefenseMap? map = null)
        => DefenseEngine.NewRun(unlocked ?? Array.Empty<string>(), map ?? MapCatalog.Default, energy, clearBonus);

    private static int TickUntilWaveEnds(DefenseState state, int maxMs = 180_000)
    {
        int elapsed = 0;
        while (state.Phase == DefensePhase.WaveRunning && elapsed < maxMs)
        {
            DefenseEngine.Tick(state, TickMs);
            elapsed += (int)TickMs;
        }

        Assert.NotEqual(DefensePhase.WaveRunning, state.Phase);
        return elapsed;
    }

    private static TowerUnlock Unlock(string towerId) => new() { ID = Guid.NewGuid(), TowerId = towerId };
}
