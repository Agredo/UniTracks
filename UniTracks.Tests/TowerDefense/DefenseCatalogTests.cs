using UniTracks.Games.TowerDefense;

namespace UniTracks.Tests.TowerDefense;

/// <summary>
/// Invariant checks over the static game data. The catalogs are hand-written and grow over time,
/// so these tests guard the assumptions the engine relies on rather than fixed numbers.
/// </summary>
public sealed class DefenseCatalogTests
{
    [Fact]
    public void MapCatalog_IsOrderedFromEasiestToHardest()
    {
        for (int i = 1; i < MapCatalog.All.Length; i++)
        {
            Assert.True(
                MapCatalog.All[i].Difficulty > MapCatalog.All[i - 1].Difficulty,
                $"{MapCatalog.All[i].Id} must be harder than {MapCatalog.All[i - 1].Id}");
        }

        Assert.Same(MapCatalog.All[0], MapCatalog.Default);
    }

    [Fact]
    public void MapCatalog_HasUniqueIds_AndAtLeastThreeWaypoints()
    {
        var ids = MapCatalog.All.Select(m => m.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(MapCatalog.All, map => Assert.True(map.Waypoints.Length >= 3, $"{map.Id} needs a real path"));
        Assert.All(MapCatalog.All, map => Assert.True(map.GridWidth > 0 && map.GridHeight > 0));
        Assert.All(MapCatalog.All, map => Assert.True(map.StartLives > 0, $"{map.Id} needs lives"));
    }

    [Fact]
    public void MapCatalog_FindIsCaseInsensitive_AndFallsBackToTheDefault()
    {
        Assert.Equal("seeufer", MapCatalog.Find("SEEUFER").Id);
        Assert.Equal("seeufer", MapCatalog.Find("seeufer").Id);
        Assert.Same(MapCatalog.Default, MapCatalog.Find("gibt-es-nicht"));
        Assert.Same(MapCatalog.Default, MapCatalog.Find(null));
    }

    [Fact]
    public void EveryMap_HasANonZeroPathAndBuildableGround()
    {
        foreach (var map in MapCatalog.All)
        {
            Assert.True(map.TotalLength > 0, $"{map.Id} has a zero-length trail");
            Assert.True(map.BuildableCount > 0, $"{map.Id} has nowhere to build");
        }
    }

    [Fact]
    public void EveryMap_StartsTheTrailAboveAndEndsItBelowTheGrid()
    {
        foreach (var map in MapCatalog.All)
        {
            // A leak is detected by "distance >= TotalLength", so the trail has to leave the grid
            // at both ends — otherwise enemies would be removed while still on screen.
            Assert.True(map.Entry.Y < 0, $"{map.Id} must enter from above the grid");
            Assert.True(map.Exit.Y > map.GridHeight, $"{map.Id} must leave below the grid");
        }
    }

    [Fact]
    public void EveryMap_KeepsItsPathOrthogonal()
    {
        foreach (var map in MapCatalog.All)
        {
            for (int i = 0; i < map.Waypoints.Length - 1; i++)
            {
                var (x1, y1) = map.Waypoints[i];
                var (x2, y2) = map.Waypoints[i + 1];
                Assert.True(
                    Math.Abs(x1 - x2) < double.Epsilon || Math.Abs(y1 - y2) < double.Epsilon,
                    $"{map.Id}: segment {i} ({x1},{y1})→({x2},{y2}) is diagonal");
            }
        }
    }

    [Fact]
    public void NoMap_BlocksItsOwnTrailWithWaterOrForest()
    {
        foreach (var map in MapCatalog.All)
        {
            foreach (var tile in map.Water)
            {
                Assert.False(map.IsPath(tile.X, tile.Y), $"{map.Id}: water tile {tile} lies on the trail");
            }

            foreach (var tile in map.Forest)
            {
                Assert.False(map.IsPath(tile.X, tile.Y), $"{map.Id}: forest tile {tile} lies on the trail");
            }
        }
    }

    [Fact]
    public void NoMap_PlacesWaterOrForestOutsideItsGrid()
    {
        foreach (var map in MapCatalog.All)
        {
            foreach (var tile in map.Water.Concat(map.Forest))
            {
                Assert.InRange(tile.X, 0, map.GridWidth - 1);
                Assert.InRange(tile.Y, 0, map.GridHeight - 1);
            }
        }
    }

    [Fact]
    public void EveryMap_HasDistinctWaterAndForestTiles()
    {
        foreach (var map in MapCatalog.All)
        {
            var overlap = map.Water.Intersect(map.Forest).ToList();
            Assert.True(overlap.Count == 0, $"{map.Id}: tiles are both water and forest: {string.Join(", ", overlap)}");
            Assert.Equal(map.Water.Length, map.Water.Distinct().Count());
            Assert.Equal(map.Forest.Length, map.Forest.Distinct().Count());
        }
    }

    [Fact]
    public void PositionAt_ClampsAtBothEndsOfTheTrail()
    {
        var map = MapCatalog.Default;

        Assert.Equal(map.Entry, map.PositionAt(-100));
        Assert.Equal(map.Exit, map.PositionAt(map.TotalLength + 100));
        Assert.Equal(map.Entry, map.PositionAt(0));
    }

    [Fact]
    public void PositionAt_WalksTheTrailMonotonically()
    {
        var map = MapCatalog.Default;
        double previousX = map.PositionAt(0).X;
        double previousY = map.PositionAt(0).Y;
        double travelled = 0;

        for (double step = 0.25; step <= map.TotalLength; step += 0.25)
        {
            var (x, y) = map.PositionAt(step);
            travelled += Math.Abs(x - previousX) + Math.Abs(y - previousY);
            previousX = x;
            previousY = y;
        }

        Assert.Equal(map.TotalLength, travelled, 6);
    }

    [Fact]
    public void TileKind_ClassifiesPathWaterForestAndGrass()
    {
        var park = MapCatalog.Find("park-promenade");

        Assert.Equal(DefenseTileKind.Path, park.TileKind(2, 0));
        Assert.Equal(DefenseTileKind.Water, park.TileKind(0, 10));
        Assert.Equal(DefenseTileKind.Forest, park.TileKind(7, 1));
        Assert.Equal(DefenseTileKind.Grass, park.TileKind(0, 0));
    }

    [Fact]
    public void EveryMap_IsGatedInAReachableWay()
    {
        Assert.True(MapCatalog.Default.RequiredLevel <= 1, "the default map must always be playable");
        Assert.Empty(MapCatalog.Default.RequiredAchievementIds);

        foreach (var map in MapCatalog.All)
        {
            Assert.True(map.RequiredLevel >= 1, $"{map.Id} has an impossible level gate");
        }
    }

    [Fact]
    public void TowerCatalog_HasUniqueIds_AndASingleFreeStarter()
    {
        var towers = TowerCatalog.Towers;
        var ids = towers.Select(t => t.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        var starter = Assert.Single(towers, t => t.IsFree);
        Assert.Equal("spray", starter.Id);
    }

    [Fact]
    public void TowerCatalog_PricesRiseWithPower()
    {
        var paid = TowerCatalog.Towers.Where(t => !t.IsFree).ToList();

        Assert.All(paid, tower => Assert.True(tower.UnlockCost > 0, $"{tower.Id} has no price"));
        Assert.All(paid, tower => Assert.True(tower.EnergyCost > 0, $"{tower.Id} is free to place"));
        Assert.All(TowerCatalog.Towers, tower => Assert.True(tower.Damage > 0 && tower.FireIntervalMs > 0));

        // Unlock prices are strictly increasing along the catalog order.
        for (int i = 1; i < paid.Count; i++)
        {
            Assert.True(paid[i].UnlockCost > paid[i - 1].UnlockCost, $"{paid[i].Id} must cost more than {paid[i - 1].Id}");
        }
    }

    [Fact]
    public void TowerCatalog_RequiresAReachableLevelForEveryTower()
    {
        Assert.All(TowerCatalog.Towers, tower => Assert.InRange(tower.RequiredLevel, 1, 10));
    }

    [Fact]
    public void TowerCatalog_FindIsExactAndReturnsNullForUnknownIds()
    {
        Assert.NotNull(TowerCatalog.Find("gecko"));
        Assert.Null(TowerCatalog.Find("Gecko")); // the catalog is matched by exact id
        Assert.Null(TowerCatalog.Find(""));
    }

    [Fact]
    public void EnemyCatalog_HasUniqueIds_AndEveryEnemyIsLethalOnALeak()
    {
        var enemies = EnemyCatalog.Enemies;

        Assert.Equal(enemies.Count, enemies.Select(e => e.Id).Distinct().Count());
        Assert.All(enemies, enemy => Assert.True(enemy.BaseHp > 0, $"{enemy.Id} has no hit points"));
        Assert.All(enemies, enemy => Assert.True(enemy.SpeedTilesPerSecond > 0, $"{enemy.Id} cannot move"));
        Assert.All(enemies, enemy => Assert.True(enemy.LeakDamage > 0, $"{enemy.Id} would be harmless on a leak"));
        Assert.All(enemies, enemy => Assert.True(enemy.ScoreReward > 0, $"{enemy.Id} awards no score"));
    }

    [Fact]
    public void EnemyCatalog_TougherEnemiesAreWorthMore()
    {
        var ordered = EnemyCatalog.Enemies.OrderBy(e => e.BaseHp).ToList();

        for (int i = 1; i < ordered.Count; i++)
        {
            Assert.True(
                ordered[i].ScoreReward > ordered[i - 1].ScoreReward,
                $"{ordered[i].Id} must be worth more than {ordered[i - 1].Id}");
        }
    }

    [Fact]
    public void WaveCatalog_ScalesHitPointsPerWave()
    {
        Assert.Equal(1.0, WaveCatalog.HpMultiplier(1), 6);
        Assert.True(WaveCatalog.HpMultiplier(2) > WaveCatalog.HpMultiplier(1));
        Assert.True(WaveCatalog.HpMultiplier(20) > WaveCatalog.HpMultiplier(10));
    }

    [Fact]
    public void WaveCatalog_WavesGrowAndStayNonEmpty()
    {
        var map = MapCatalog.Default;

        for (int wave = 1; wave <= 25; wave++)
        {
            var enemies = WaveCatalog.For(wave, map);
            Assert.NotEmpty(enemies);
            Assert.All(enemies, enemy => Assert.NotNull(EnemyCatalog.Find(enemy.Id)));
        }

        Assert.True(WaveCatalog.For(10, map).Count > WaveCatalog.For(1, map).Count);
    }

    [Fact]
    public void WaveCatalog_SendsABossEveryFifthWave()
    {
        var map = MapCatalog.Default;

        for (int wave = 1; wave <= 30; wave++)
        {
            var enemies = WaveCatalog.For(wave, map);
            bool hasHornet = enemies.Any(e => e.Id == "hornet");

            Assert.Equal(wave % 5 == 0, hasHornet);
            Assert.Equal(hasHornet ? 1 : 0, enemies.Count(e => e.Id == "hornet"));
        }
    }

    [Fact]
    public void WaveCatalog_HarderMapsSendMoreEnemies()
    {
        int easy = WaveCatalog.For(3, MapCatalog.Find("waldwiese")).Count;
        int hard = WaveCatalog.For(3, MapCatalog.Find("streifzug")).Count;

        Assert.True(hard > easy, "a harder map must add extra swarmers");
    }
}
