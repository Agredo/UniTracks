namespace UniTracks.Games.TowerDefense;

/// <summary>A projectile in flight, homing in on its target enemy.</summary>
public class ActiveProjectile
{
    public double X { get; set; }

    public double Y { get; set; }

    /// <summary>Tile-space origin of the shot — the firing tower's center (renders the effect stem).</summary>
    public double OriginX { get; init; }

    public double OriginY { get; init; }

    /// <summary>Runtime id of the targeted <see cref="ActiveEnemy"/>.</summary>
    public int TargetEnemyId { get; init; }

    /// <summary>Travel speed in tiles per second (copied from the firing tower).</summary>
    public double Speed { get; init; }

    public int Damage { get; init; }

    /// <summary>Render color (hex), copied from the firing tower.</summary>
    public string ColorHex { get; init; } = "#FFFFFF";

    /// <summary>How the shot is rendered — copied from the firing tower.</summary>
    public AttackStyle AttackStyle { get; init; }
}
