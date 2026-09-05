namespace UniTracks.Games.TowerDefense;

/// <summary>A tower placed on the grid during a run, with its remaining fire cooldown.</summary>
public class PlacedTower
{
    public int X { get; init; }

    public int Y { get; init; }

    /// <summary>References <see cref="TowerDefinition.Id"/> from the static catalog.</summary>
    public string TowerId { get; init; } = string.Empty;

    /// <summary>Milliseconds until the tower can fire again.</summary>
    public double CooldownRemainingMs { get; set; }
}
