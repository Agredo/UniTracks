using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Games.Shared.Economy;

/// <summary>
/// Coin economy rules. Coins are earned through real activity (trips, achievements)
/// and spent on buildings. The balance is always computed — never stored — so it
/// can never drift out of sync with the underlying trip data.
/// </summary>
public static class CoinEconomy
{
    public const int CoinsPerKm = 10;

    public const int CoinsPerTrip = 5;

    public const int CoinsPerAchievement = 25;

    /// <summary>Welcome bonus every player starts with, so the first buildings are affordable right away.</summary>
    public const int StartingCoins = 500;

    /// <summary>Fraction of the building cost refunded on demolition.</summary>
    public const double DemolitionRefundFraction = 0.5;

    /// <summary>Coins awarded per gamification level reached (multiplied by the level number).</summary>
    public const int CoinsPerLevel = 100;

    /// <summary>XP needed per gamification level (mirrors the gamification service).</summary>
    public const int XpPerLevel = 100;

    /// <summary>
    /// Total coins earned so far (includes the starting bonus). Distance coins scale with the
    /// per-trip-type effort factor (<see cref="TripTypeFactors"/>), so a 20 km bike ride earns
    /// far less than a 20 km run. Level-ups grant a growing bonus: 100×2 + 100×3 + … + 100×level.
    /// </summary>
    public static int ComputeEarned(
        IEnumerable<TripActivity> trips,
        int xp,
        int unlockedAchievements)
    {
        int distanceCoins = 0;
        int tripCoins = 0;
        foreach (var trip in trips)
        {
            distanceCoins += (int)Math.Floor(trip.DistanceKm * TripTypeFactors.For(trip.Category, trip.Identifier) * CoinsPerKm);
            tripCoins += CoinsPerTrip;
        }

        int level = xp / XpPerLevel + 1;
        int levelBonus = level > 1 ? CoinsPerLevel * (level - 1) * (level + 2) / 2 : 0; // 100×(2+3+…+level)

        return StartingCoins
            + distanceCoins
            + tripCoins
            + levelBonus
            + unlockedAchievements * CoinsPerAchievement;
    }

    public static int DemolitionRefund(int cost) => (int)Math.Floor(cost * DemolitionRefundFraction);
}
