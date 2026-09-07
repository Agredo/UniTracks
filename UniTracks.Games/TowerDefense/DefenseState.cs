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

    /// <summary>The map this run is played on (drives the trail geometry and difficulty modifiers).</summary>
    public DefenseMap Map { get; init; } = MapCatalog.Default;

    /// <summary>In-run placement currency, topped up by the sport-based wave clear bonus and coin-funded top-ups.</summary>
    public int Energy { get; set; }

    /// <summary>Sport-based energy returned after each cleared wave (see <see cref="Shared.Economy.EnergyEconomy"/>).</summary>
    public int ClearBonus { get; set; }

    public int Lives { get; set; }

    public int Score { get; set; }

    /// <summary>Number of the wave that will start next (1-based).</summary>
    public int NextWave { get; set; } = 1;

    /// <summary>Highest wave number fully cleared with zero leaks (0 = none yet).</summary>
    public int BestClearWave { get; set; }

    /// <summary>The score at the moment the zero-leak best wave (<see cref="BestClearWave"/>) was cleared.</summary>
    public int BestClearScore { get; set; }

    /// <summary>Enemies spawned so far in the current wave.</summary>
    public int WaveSpawned { get; set; }

    /// <summary>Enemies that reached the end of the trail in the current wave.</summary>
    public int WaveLeaked { get; set; }

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

    /// <summary>True when the tile is inside the grid, is plain grass (no trail/water/forest) and not occupied.</summary>
    public bool IsBuildable(int x, int y) =>
        x >= 0 && x < Map.GridWidth
        && y >= 0 && y < Map.GridHeight
        && Map.TileKind(x, y) == DefenseTileKind.Grass
        && TowerAt(x, y) is null;
}
