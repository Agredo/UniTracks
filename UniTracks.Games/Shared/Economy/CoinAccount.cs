namespace UniTracks.Games.Shared.Economy;

/// <summary>
/// The player's single coin account, shared by every game: everything earned through real
/// activity minus everything spent anywhere. Spending in one game therefore immediately
/// reduces the balance the other games show and enforce.
/// </summary>
public record CoinAccount
{
    /// <summary>Coins earned through trips, level-ups and achievements (includes the welcome bonus).</summary>
    public int Earned { get; init; }

    /// <summary>Coins invested in the city builder — buildings and grid expansions.</summary>
    public int CitySpent { get; init; }

    /// <summary>Coins invested in the tower defense game — tower unlocks and coin-funded energy.</summary>
    public int TowerDefenseSpent { get; init; }

    /// <summary>Coins spent across all games.</summary>
    public int Spent => CitySpent + TowerDefenseSpent;

    /// <summary>Coins available right now — the same number in every game.</summary>
    public int Balance => Math.Max(0, Earned - Spent);
}
