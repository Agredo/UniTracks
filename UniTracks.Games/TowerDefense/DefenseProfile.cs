namespace UniTracks.Games.TowerDefense;

/// <summary>
/// Snapshot of the player's persistent trail-defense progress: spendable coin balance,
/// unlocked towers and the best result so far. Loaded by the service layer whenever
/// the game screen is shown or a purchase changes the balance.
/// </summary>
public record DefenseProfile
{
    /// <summary>Spendable coins (earned from activity minus all game spending).</summary>
    public int Coins { get; init; }

    public IReadOnlyList<string> UnlockedTowerIds { get; init; } = Array.Empty<string>();

    /// <summary>Highest wave ever fully cleared (0 = no finished run yet).</summary>
    public int BestWave { get; init; }

    public int BestScore { get; init; }
}
