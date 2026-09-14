using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.BaseCamp.Persistence;

/// <summary>
/// The camp's ledger (single row): the harvest anchor plus the supplies that were already
/// banked. Production itself is never stored — it is reconstructed from the anchor and the
/// player's activity, so this row only has to record what has been harvested and spent.
/// </summary>
public record CampLog
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>Timestamp the supplies currently in the camp accrue from; older time was harvested.</summary>
    public DateTimeOffset LastCollectedAt { get; set; }

    /// <summary>Supplies harvested over the camp's lifetime.</summary>
    public int TotalCollected { get; set; }

    /// <summary>
    /// Spendable supplies: every harvest adds to it, every module upgrade subtracts from it.
    /// Kept separate from <see cref="LastCollectedAt"/> because the camp would otherwise be
    /// unable to charge for anything — its stock is only ever a function of time.
    /// </summary>
    public int BankedSupplies { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
