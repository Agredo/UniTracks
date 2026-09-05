namespace UniTracks.Games.TowerDefense;

/// <summary>An enemy currently on the trail, positioned by its travelled distance.</summary>
public class ActiveEnemy
{
    /// <summary>Runtime identity — projectiles track their target by this id.</summary>
    public int Id { get; init; }

    public EnemyDefinition Definition { get; init; } = new();

    /// <summary>Wave-scaled hit points this enemy spawned with (for health-bar rendering).</summary>
    public int MaxHp { get; init; }

    public int Hp { get; set; }

    /// <summary>Distance travelled along the path in tile units (see <see cref="DefensePath"/>).</summary>
    public double Distance { get; set; }

    /// <summary>Current position on the grid, derived from <see cref="Distance"/>.</summary>
    public (double X, double Y) Position => DefensePath.PositionAt(Distance);
}
