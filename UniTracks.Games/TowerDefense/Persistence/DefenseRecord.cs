using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.TowerDefense.Persistence;

/// <summary>
/// The player's best trail-defense result (single row). Updated whenever a finished
/// run beats the stored wave or score.
/// </summary>
public record DefenseRecord
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>Highest wave number that was fully cleared.</summary>
    public int BestWave { get; set; }

    public int BestScore { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
