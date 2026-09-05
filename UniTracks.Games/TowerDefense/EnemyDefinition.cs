namespace UniTracks.Games.TowerDefense;

/// <summary>Static definition of an enemy type marching down the trail.</summary>
public record EnemyDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Icon { get; init; } = "🦟";

    /// <summary>Base hit points (scaled up per wave by <see cref="WaveCatalog"/>).</summary>
    public int BaseHp { get; init; }

    /// <summary>Movement speed in tiles per second.</summary>
    public double SpeedTilesPerSecond { get; init; }

    /// <summary>Energy awarded for a kill (the in-run placement currency).</summary>
    public int EnergyReward { get; init; }

    /// <summary>Score points awarded for a kill (feeds the highscore).</summary>
    public int ScoreReward { get; init; }

    /// <summary>Lives lost when this enemy reaches the end of the trail.</summary>
    public int LeakDamage { get; init; } = 1;
}
