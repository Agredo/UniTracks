using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Games.Shared.Economy;

/// <summary>
/// In-run energy rules for the tower-defense game. Energy is the placement currency that
/// lets a player build towers during a run. Unlike the coin balance, energy is not earned
/// by killing enemies — clearing a wave only trickles a small, sport-based amount back.
/// The budget therefore grows with the player's real activity (level-ups from XP,
/// achievements, lifetime kilometers) rather than with in-game kills, so progress is tied
/// to running and exercising, not to grinding a wave. A player may additionally top the
/// budget up with coins (a durable spend that is part of the computed coin balance).
/// </summary>
public static class EnergyEconomy
{
    /// <summary>Enough to place a couple of starter towers and clear the first wave or two.</summary>
    public const int StartingEnergy = 60;

    /// <summary>Coins paid for each point of energy bought with coin during a run.</summary>
    public const int CoinsPerEnergy = 2;

    /// <summary>Energy granted by a single coin-funded purchase (the buy button in the HUD).</summary>
    public const int EnergyPackSize = 25;

    /// <summary>Coins a coin-funded energy purchase of <paramref name="energy"/> points costs.</summary>
    public static int CoinCostForEnergy(int energy) => energy * CoinsPerEnergy;

    /// <summary>Floor of energy trickled back after each cleared wave, so a brand-new player is never hard-stuck.</summary>
    public const int BaseClearBonus = 20;

    /// <summary>Extra energy per cleared wave for each gamification level above level 1.</summary>
    public const int EnergyPerLevel = 10;

    /// <summary>Extra energy per cleared wave for each unlocked achievement.</summary>
    public const int EnergyPerAchievement = 15;

    /// <summary>Extra energy per cleared wave for each 10 lifetime kilometers.</summary>
    public const int EnergyPer10Km = 5;

    /// <summary>Upper bound on the per-wave trickle, so late waves stay challenging.</summary>
    public const int ClearBonusCap = 150;

    /// <summary>Energy every run starts with — a fixed credit, independent of activity.</summary>
    public static int ComputeStartingEnergy(ActivityStats stats) => StartingEnergy;

    /// <summary>
    /// Sport-based energy returned after each cleared wave. Grows with level-ups (which are
    /// driven by XP, i.e. kilometers and trips), unlocked achievements and lifetime distance,
    /// mirroring <see cref="CoinEconomy"/> so in-game effort can never substitute for running.
    /// </summary>
    public static int ComputeClearBonus(ActivityStats stats)
    {
        int levelBonus = Math.Max(0, stats.Level - 1) * EnergyPerLevel;
        int achievementBonus = stats.UnlockedAchievements * EnergyPerAchievement;
        int kmBonus = (int)(stats.TotalDistanceKm / 10) * EnergyPer10Km;
        return Math.Min(ClearBonusCap, BaseClearBonus + levelBonus + achievementBonus + kmBonus);
    }
}
