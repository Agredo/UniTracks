using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.TowerDefense.Persistence;

/// <summary>
/// Persisted snapshot of the player's in-progress defense run. On reload the tower layout, current
/// wave and the energy budget (including any coin-funded top-ups) are restored. Only wave boundaries
/// are persisted: enemies, projectiles and pending spawns are runtime-only, so a snapshot taken while
/// a wave was running is restored at the start of that wave (and the score of the aborted attempt is
/// not kept, because those kills are earned again when the wave is replayed). A single row keeps the
/// run as one unit and works the same on EF Core (SQLite) and LiteDB (iOS).
/// </summary>
public record DefenseRunProgress
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>Number of the wave the player must clear next (1-based).</summary>
    public int Wave { get; set; } = 1;

    /// <summary>Id of the map the run is played on (see <c>MapCatalog</c>). Defaults to the easiest map.</summary>
    public string MapId { get; set; } = MapCatalog.Default.Id;

    /// <summary>Current in-run energy budget, persisted so purchased energy survives a resume.</summary>
    public int Energy { get; set; }

    /// <summary>
    /// Remaining lives. Zero means the run is over: such a snapshot is not offered for resuming,
    /// because restoring it would refund the starting energy while keeping the failed layout.
    /// </summary>
    public int Lives { get; set; }

    /// <summary>Score at the wave boundary this snapshot describes.</summary>
    public int Score { get; set; }

    /// <summary>Highest wave fully cleared with zero leaks in this run (0 = none yet).</summary>
    public int BestClearWave { get; set; }

    /// <summary>The score at the moment the zero-leak best wave was cleared.</summary>
    public int BestClearScore { get; set; }

    /// <summary>JSON-serialized list of <see cref="PlacedTower"/> of the current layout.</summary>
    public string TowersJson { get; set; } = "[]";

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
