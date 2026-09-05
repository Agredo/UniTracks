using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.TowerDefense.Persistence;

/// <summary>
/// A permanently unlocked tower type, purchased with coins. Coin balance is always
/// computed (earned from activity minus all spending), so unlocks are the only
/// persisted tower state. Works on EF Core (SQLite) as well as LiteDB on iOS.
/// </summary>
public record TowerUnlock
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>References <c>TowerDefinition.Id</c> from the static catalog.</summary>
    public string TowerId { get; init; } = string.Empty;

    public DateTimeOffset PurchasedAt { get; init; } = DateTimeOffset.UtcNow;
}
