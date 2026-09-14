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

    /// <summary>Coins invested in the base camp — module upgrades priced in coins.</summary>
    public int CampSpent { get; init; }

    /// <summary>Coins spent across all games.</summary>
    public int Spent => CitySpent + TowerDefenseSpent + CampSpent;

    /// <summary>
    /// Coins spent outside the city builder. Each game rebuilds its own spend from its own
    /// entities, so it must subtract exactly the other games' spending — never the total,
    /// which would count its own spending twice.
    /// </summary>
    public int SpentOutsideCity => TowerDefenseSpent + CampSpent;

    /// <summary>Coins spent outside the base camp (same reasoning as <see cref="SpentOutsideCity"/>).</summary>
    public int SpentOutsideCamp => CitySpent + TowerDefenseSpent;

    /// <summary>Coins available right now — the same number in every game.</summary>
    public int Balance => Math.Max(0, Earned - Spent);
}
