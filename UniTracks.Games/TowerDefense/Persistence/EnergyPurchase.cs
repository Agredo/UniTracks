using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.TowerDefense.Persistence;

/// <summary>
/// A coin-funded energy purchase made during a run. Energy itself is a per-run budget,
/// but the coins spent are durable — recorded here so the always-computed coin balance
/// (earned from activity minus all spending) stays in sync. Works on EF Core (SQLite)
/// as well as LiteDB on iOS.
/// </summary>
public record EnergyPurchase
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>Energy points granted to the run.</summary>
    public int Energy { get; init; }

    /// <summary>Coins paid for this purchase.</summary>
    public int Coins { get; init; }

    public DateTimeOffset PurchasedAt { get; init; } = DateTimeOffset.UtcNow;
}
