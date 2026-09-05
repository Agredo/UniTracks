namespace UniTracks.Games.TowerDefense;

/// <summary>A projectile in flight, homing in on its target enemy.</summary>
public class ActiveProjectile
{
    public double X { get; set; }

    public double Y { get; set; }

    /// <summary>Runtime id of the targeted <see cref="ActiveEnemy"/>.</summary>
    public int TargetEnemyId { get; init; }

    /// <summary>Travel speed in tiles per second (copied from the firing tower).</summary>
    public double Speed { get; init; }

    public int Damage { get; init; }

    /// <summary>Render color (hex), copied from the firing tower.</summary>
    public string ColorHex { get; init; } = "#FFFFFF";
}
