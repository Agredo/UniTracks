namespace UniTracks.Games.BaseCamp;

/// <summary>
/// Complete state of the player's base camp. Production is derived from the last harvest
/// timestamp and the player's real activity — it is never persisted, exactly like the coin
/// balance, so the camp can never drift out of sync with the recorded trips. Only what has
/// been harvested and spent is stored.
/// </summary>
public record CampState
{
    /// <summary>Supplies waiting in the camp right now — what a harvest banks.</summary>
    public int Stock { get; init; }

    /// <summary>Supplies the player owns and can spend on modules (harvested, minus spent).</summary>
    public int Supplies { get; init; }

    /// <summary>Upper bound of the camp stock.</summary>
    public int Capacity { get; init; } = CampEconomy.BaseCapacity;

    /// <summary>Supplies produced per hour at this moment.</summary>
    public double RatePerHour { get; init; }

    /// <summary>Weighted kilometres of the rolling week feeding the rate.</summary>
    public double WeightedWeeklyKm { get; init; }

    /// <summary>Multiplier from the current streak and the radio.</summary>
    public double StreakFactor { get; init; } = 1;

    /// <summary>Production factor after a pause (1 = fully fresh).</summary>
    public double Freshness { get; init; } = 1;

    /// <summary>Timestamp the current stock accrued from.</summary>
    public DateTimeOffset LastCollectedAt { get; init; }

    /// <summary>Supplies harvested over the camp's lifetime.</summary>
    public int TotalCollected { get; init; }

    /// <summary>Fill level of the camp in 0…1, drives the progress bar.</summary>
    public double FillRatio => Capacity <= 0 ? 0 : Math.Clamp((double)Stock / Capacity, 0, 1);

    /// <summary>Spendable coins (earned − spent in every game, shared account).</summary>
    public int Coins { get; init; }

    /// <summary>Total coins ever earned through trips, levels and achievements.</summary>
    public int CoinsEarned { get; init; }

    /// <summary>Total coins spent across all games, including this camp's module upgrades.</summary>
    public int CoinsSpent { get; init; }

    /// <summary>Gamification level — gates modules.</summary>
    public int Level { get; init; } = 1;

    /// <summary>Gamification XP.</summary>
    public int Xp { get; init; }

    /// <summary>Ids of unlocked achievements — gate prestige modules.</summary>
    public IReadOnlyList<string> UnlockedAchievementIds { get; init; } = Array.Empty<string>();

    /// <summary>Consecutive active days ending today or yesterday.</summary>
    public int CurrentStreakDays { get; init; }

    /// <summary>Longest streak ever reached.</summary>
    public int BestStreakDays { get; init; }

    /// <summary>Level of every built module (unbuilt modules are simply absent).</summary>
    public IReadOnlyList<CampModuleLevel> Modules { get; init; } = Array.Empty<CampModuleLevel>();

    /// <summary>True when nothing more can be produced until the camp is harvested.</summary>
    public bool IsFull => Stock >= Capacity;

    /// <summary>Level of one module (0 when it was never built).</summary>
    public int LevelOf(string moduleId) => CampEconomy.LevelOf(Modules, moduleId);

    /// <summary>True when the module cannot be upgraded any further.</summary>
    public bool IsMaxed(CampModuleDefinition module) => LevelOf(module.Id) >= module.MaxLevel;

    /// <summary>True when the module is available to this player (level + achievement gates).</summary>
    public bool IsUnlocked(CampModuleDefinition module) =>
        Level >= module.RequiredLevel
        && (module.RequiredAchievementId is null || UnlockedAchievementIds.Contains(module.RequiredAchievementId));

    /// <summary>Price of the next level, or <see cref="CampCost.Free"/> when maxed out.</summary>
    public CampCost NextCost(CampModuleDefinition module) => module.CostFor(LevelOf(module.Id) + 1);

    /// <summary>True when the next level is unlocked and both currencies suffice.</summary>
    public bool CanUpgrade(CampModuleDefinition module)
    {
        if (IsMaxed(module) || !IsUnlocked(module))
        {
            return false;
        }

        var cost = NextCost(module);
        return Coins >= cost.Coins && Supplies >= cost.Supplies;
    }
}
