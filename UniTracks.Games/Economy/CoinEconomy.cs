namespace UniTracks.Games.Economy;

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

    /// <summary>Total coins earned so far for the given activity numbers (includes the starting bonus).</summary>
    public static int ComputeEarned(double totalDistanceKm, int totalTrips, int unlockedAchievements) =>
        StartingCoins
        + (int)Math.Floor(totalDistanceKm) * CoinsPerKm
        + totalTrips * CoinsPerTrip
        + unlockedAchievements * CoinsPerAchievement;

    public static int DemolitionRefund(int cost) => (int)Math.Floor(cost * DemolitionRefundFraction);
}
