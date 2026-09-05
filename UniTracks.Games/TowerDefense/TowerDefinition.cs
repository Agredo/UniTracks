namespace UniTracks.Games.TowerDefense;

/// <summary>
/// Static definition of a tower type. Towers are unlocked permanently with coins
/// (financed by real activity) and placed during a run with energy earned from kills.
/// </summary>
public record TowerDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = "🗼";

    /// <summary>One-time coin price to unlock the tower permanently (0 = starter tower).</summary>
    public int UnlockCost { get; init; }

    /// <summary>Energy price to place the tower during a run.</summary>
    public int EnergyCost { get; init; }

    /// <summary>Attack range in tile units (center to center).</summary>
    public double RangeTiles { get; init; }

    /// <summary>Damage dealt per projectile hit.</summary>
    public int Damage { get; init; }

    /// <summary>Milliseconds between two shots.</summary>
    public int FireIntervalMs { get; init; }

    /// <summary>Projectile travel speed in tiles per second.</summary>
    public double ProjectileSpeed { get; init; } = 6;

    /// <summary>Render color (hex) used for the tower base and its projectiles.</summary>
    public string ColorHex { get; init; } = "#8BC34A";

    /// <summary>How the attack is rendered — gives every tower a distinct effect.</summary>
    public AttackStyle AttackStyle { get; init; }

    /// <summary>Starter towers need no coin unlock and are always placeable.</summary>
    public bool IsFree => UnlockCost == 0;
}
