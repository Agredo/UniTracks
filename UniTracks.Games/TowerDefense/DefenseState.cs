namespace UniTracks.Games.TowerDefense;

/// <summary>
/// Mutable runtime state of one defense run. Created via <see cref="DefenseEngine.NewRun"/>
/// and advanced exclusively through <see cref="DefenseEngine.Tick"/> — never persisted;
/// only tower unlocks and the best result survive a run.
/// </summary>
public class DefenseState
{
    public List<PlacedTower> Towers { get; } = new();

    public List<ActiveEnemy> Enemies { get; } = new();

    public List<ActiveProjectile> Projectiles { get; } = new();

    /// <summary>Tower ids the player has permanently unlocked (drives placement validation).</summary>
    public IReadOnlyList<string> UnlockedTowerIds { get; init; } = Array.Empty<string>();

    /// <summary>In-run placement currency, earned by killing enemies and clearing waves.</summary>
    public int Energy { get; set; }

    public int Lives { get; set; }

    public int Score { get; set; }

    /// <summary>Number of the wave that will start next (1-based).</summary>
    public int NextWave { get; set; } = 1;

    public DefensePhase Phase { get; set; } = DefensePhase.Building;

    /// <summary>Enemies of the running wave that still need to spawn.</summary>
    public Queue<EnemyDefinition> PendingSpawns { get; } = new();

    /// <summary>Milliseconds until the next pending enemy spawns.</summary>
    public double SpawnCooldownMs { get; set; }

    /// <summary>Number of the last wave that was fully cleared (0 = none yet).</summary>
    public int ClearedWave => Phase == DefensePhase.Building ? NextWave - 1 : NextWave;

    /// <summary>Monotonic runtime id source for spawned enemies (projectile targeting).</summary>
    public int NextEnemyId { get; set; } = 1;

    public PlacedTower? TowerAt(int x, int y) => Towers.FirstOrDefault(t => t.X == x && t.Y == y);

    /// <summary>True when the tile is inside the grid, off the trail and not occupied.</summary>
    public bool IsBuildable(int x, int y) =>
        x >= 0 && x < DefensePath.GridWidth
        && y >= 0 && y < DefensePath.GridHeight
        && !DefensePath.IsPath(x, y)
        && TowerAt(x, y) is null;
}
