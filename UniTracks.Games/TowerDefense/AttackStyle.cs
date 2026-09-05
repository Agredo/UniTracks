namespace UniTracks.Games.TowerDefense;

/// <summary>How a tower's attack is rendered — drives the distinct effect per tower type.</summary>
public enum AttackStyle
{
    /// <summary>Short-range aerosol/mist burst (Mückenspray).</summary>
    Spray,

    /// <summary>Drifting incense/scent cloud (Duftkerze).</summary>
    Cloud,

    /// <summary>Instant lightning bolt (Elektro-Falle).</summary>
    Zap,

    /// <summary>Snapping tongue (Frosch, Gecko).</summary>
    Tongue,
}
