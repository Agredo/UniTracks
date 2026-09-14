using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.BaseCamp.Persistence;

/// <summary>
/// The camp's harvest anchor (single row). Supplies are never stored — they are
/// reconstructed from this timestamp and the player's activity, so this row is the only
/// thing a harvest has to write.
/// </summary>
public record CampLog
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>Timestamp the current stock accrues from; older time has been harvested.</summary>
    public DateTimeOffset LastCollectedAt { get; set; }

    /// <summary>Supplies harvested over the camp's lifetime.</summary>
    public int TotalCollected { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
