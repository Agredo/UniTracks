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

    /// <summary>Gamification level (XP/100 + 1) — gates higher-tier tower unlocks.</summary>
    public int Level { get; init; } = 1;

    /// <summary>Ids of unlocked achievements — gate prestige tower unlocks.</summary>
    public IReadOnlyList<string> UnlockedAchievementIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> UnlockedTowerIds { get; init; } = Array.Empty<string>();

    /// <summary>Highest wave ever fully cleared (0 = no finished run yet).</summary>
    public int BestWave { get; init; }

    public int BestScore { get; init; }

    /// <summary>Energy a fresh run starts with, derived from the player's activity.</summary>
    public int StartingEnergy { get; init; }

    /// <summary>Sport-based energy returned after each cleared wave.</summary>
    public int ClearBonus { get; init; }
}
