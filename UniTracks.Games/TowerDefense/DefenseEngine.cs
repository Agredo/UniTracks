using UniTracks.Games.TowerDefense.Persistence;

namespace UniTracks.Games.TowerDefense;

/// <summary>
/// Pure game logic for the trail defense game. No MAUI, SkiaSharp or EF dependencies —
/// creates runs, validates placement/sell attempts and advances the simulation via
/// <see cref="Tick"/>. Only unlocked towers and the best result are persisted;
/// the run itself is pure runtime state.
/// </summary>
public static class DefenseEngine
{
    /// <summary>Energy every run starts with, so the first towers are placeable right away.</summary>
    public const int StartingEnergy = 100;

    public const int StartingLives = 20;

    /// <summary>Fraction of the energy cost refunded when selling a tower.</summary>
    public const double SellRefundFraction = 0.5;

    /// <summary>Distance in tile units below which a projectile counts as a hit.</summary>
    private const double HitRadius = 0.25;

    /// <summary>Starts a fresh run for a player with the given permanently unlocked towers.</summary>
    public static DefenseState NewRun(IEnumerable<string> unlockedTowerIds) => new()
    {
        UnlockedTowerIds = unlockedTowerIds.ToList(),
        Energy = StartingEnergy,
        Lives = StartingLives,
    };

    /// <summary>Coins permanently invested in tower unlocks (feeds the shared coin balance).</summary>
    public static int ComputeUnlockSpent(IEnumerable<TowerUnlock> unlocks) =>
        unlocks.Sum(u => TowerCatalog.Find(u.TowerId)?.UnlockCost ?? 0);

    /// <summary>Validates a tower placement. Does not mutate anything.</summary>
    public static DefenseResult ValidatePlacement(DefenseState state, string towerId, int x, int y)
    {
        var tower = TowerCatalog.Find(towerId);
        if (tower is null)
        {
            return DefenseResult.Fail(DefenseError.UnknownTower);
        }

        if (!tower.IsFree && !state.UnlockedTowerIds.Contains(towerId))
        {
            return DefenseResult.Fail(DefenseError.TowerLocked);
        }

        if (x < 0 || x >= DefensePath.GridWidth || y < 0 || y >= DefensePath.GridHeight)
        {
            return DefenseResult.Fail(DefenseError.OutOfBounds);
        }

        if (DefensePath.IsPath(x, y))
        {
            return DefenseResult.Fail(DefenseError.NotBuildable);
        }

        if (state.TowerAt(x, y) is not null)
        {
            return DefenseResult.Fail(DefenseError.TileOccupied);
        }

        if (state.Energy < tower.EnergyCost)
        {
            return DefenseResult.Fail(DefenseError.NotEnoughEnergy);
        }

        return DefenseResult.Ok(-tower.EnergyCost);
    }

    /// <summary>Validates and places a tower, spending energy on success.</summary>
    public static DefenseResult PlaceTower(DefenseState state, string towerId, int x, int y)
    {
        var validation = ValidatePlacement(state, towerId, x, y);
        if (!validation.Success)
        {
            return validation;
        }

        state.Towers.Add(new PlacedTower { X = x, Y = y, TowerId = towerId });
        state.Energy += validation.EnergyDelta;
        return validation;
    }

    /// <summary>Validates and sells a tower, refunding half of its energy cost.</summary>
    public static DefenseResult SellTower(DefenseState state, int x, int y)
    {
        var tower = state.TowerAt(x, y);
        if (tower is null)
        {
            return DefenseResult.Fail(DefenseError.TileEmpty);
        }

        int refund = (int)Math.Floor((TowerCatalog.Find(tower.TowerId)?.EnergyCost ?? 0) * SellRefundFraction);
        state.Towers.Remove(tower);
        state.Energy += refund;
        return DefenseResult.Ok(refund);
    }

    /// <summary>Queues the next wave and switches the run into <see cref="DefensePhase.WaveRunning"/>.</summary>
    public static DefenseResult StartWave(DefenseState state)
    {
        if (state.Phase == DefensePhase.WaveRunning)
        {
            return DefenseResult.Fail(DefenseError.WaveRunning);
        }

        foreach (var enemy in WaveCatalog.For(state.NextWave))
        {
            state.PendingSpawns.Enqueue(enemy);
        }

        state.SpawnCooldownMs = 0;
        state.Phase = DefensePhase.WaveRunning;
        return DefenseResult.Ok(0);
    }

    /// <summary>
    /// Advances the simulation by <paramref name="deltaMs"/>: spawning, enemy movement,
    /// tower fire, projectile hits, kills, leaks and wave/run transitions.
    /// </summary>
    public static void Tick(DefenseState state, double deltaMs)
    {
        if (state.Phase != DefensePhase.WaveRunning)
        {
            return;
        }

        double deltaSeconds = deltaMs / 1000.0;

        SpawnEnemies(state, deltaMs);
        MoveEnemies(state, deltaSeconds);
        FireTowers(state, deltaMs);
        MoveProjectiles(state, deltaSeconds);

        if (state.Lives <= 0)
        {
            state.Lives = 0;
            state.Phase = DefensePhase.Lost;
            state.Enemies.Clear();
            state.Projectiles.Clear();
            state.PendingSpawns.Clear();
            return;
        }

        if (state.PendingSpawns.Count == 0 && state.Enemies.Count == 0)
        {
            state.Energy += WaveCatalog.ClearBonus(state.NextWave);
            state.NextWave++;
            state.Phase = DefensePhase.Building;
        }
    }

    private static void SpawnEnemies(DefenseState state, double deltaMs)
    {
        state.SpawnCooldownMs -= deltaMs;
        double hpMultiplier = WaveCatalog.HpMultiplier(state.NextWave);

        while (state.PendingSpawns.Count > 0 && state.SpawnCooldownMs <= 0)
        {
            var definition = state.PendingSpawns.Dequeue();
            int maxHp = (int)Math.Ceiling(definition.BaseHp * hpMultiplier);
            state.Enemies.Add(new ActiveEnemy
            {
                Id = state.NextEnemyId++,
                Definition = definition,
                MaxHp = maxHp,
                Hp = maxHp,
                Distance = 0,
            });

            state.SpawnCooldownMs += WaveCatalog.SpawnIntervalMs;
        }
    }

    private static void MoveEnemies(DefenseState state, double deltaSeconds)
    {
        for (int i = state.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = state.Enemies[i];
            enemy.Distance += enemy.Definition.SpeedTilesPerSecond * deltaSeconds;
            if (enemy.Distance >= DefensePath.TotalLength)
            {
                state.Lives -= enemy.Definition.LeakDamage;
                state.Enemies.RemoveAt(i);
                state.Projectiles.RemoveAll(p => p.TargetEnemyId == enemy.Id);
            }
        }
    }

    private static void FireTowers(DefenseState state, double deltaMs)
    {
        foreach (var tower in state.Towers)
        {
            tower.CooldownRemainingMs -= deltaMs;
            if (tower.CooldownRemainingMs > 0)
            {
                continue;
            }

            var definition = TowerCatalog.Find(tower.TowerId);
            if (definition is null)
            {
                continue;
            }

            // Target the enemy furthest along the trail within range.
            var target = state.Enemies
                .Where(e => DistanceTo(tower, e) <= definition.RangeTiles)
                .OrderByDescending(e => e.Distance)
                .FirstOrDefault();
            if (target is null)
            {
                tower.CooldownRemainingMs = 0;
                continue;
            }

            state.Projectiles.Add(new ActiveProjectile
            {
                X = tower.X + 0.5,
                Y = tower.Y + 0.5,
                TargetEnemyId = target.Id,
                Speed = definition.ProjectileSpeed,
                Damage = definition.Damage,
                ColorHex = definition.ColorHex,
            });

            tower.CooldownRemainingMs = definition.FireIntervalMs;
        }
    }

    private static void MoveProjectiles(DefenseState state, double deltaSeconds)
    {
        for (int i = state.Projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = state.Projectiles[i];
            var target = state.Enemies.FirstOrDefault(e => e.Id == projectile.TargetEnemyId);
            if (target is null)
            {
                state.Projectiles.RemoveAt(i);
                continue;
            }

            var (tx, ty) = target.Position;
            double dx = tx - projectile.X;
            double dy = ty - projectile.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double step = projectile.Speed * deltaSeconds;

            if (distance <= Math.Max(step, HitRadius))
            {
                target.Hp -= projectile.Damage;
                state.Projectiles.RemoveAt(i);
                if (target.Hp <= 0)
                {
                    state.Energy += target.Definition.EnergyReward;
                    state.Score += target.Definition.ScoreReward;
                    state.Enemies.Remove(target);
                    state.Projectiles.RemoveAll(p => p.TargetEnemyId == target.Id);
                }
            }
            else
            {
                projectile.X += dx / distance * step;
                projectile.Y += dy / distance * step;
            }
        }
    }

    private static double DistanceTo(PlacedTower tower, ActiveEnemy enemy)
    {
        var (ex, ey) = enemy.Position;
        double dx = ex - (tower.X + 0.5);
        double dy = ey - (tower.Y + 0.5);
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
