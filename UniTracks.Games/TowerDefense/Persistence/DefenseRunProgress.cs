using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.TowerDefense.Persistence;

/// <summary>
/// Persisted snapshot of the player's in-progress defense run. On reload the tower
/// layout and current wave are restored, while energy is freshly recomputed from the
/// player's activity (see <c>UniTracks.Games.Shared.Economy.EnergyEconomy</c>) so that
/// progress always requires sport. A single row keeps the run as one unit and works the
/// same on EF Core (SQLite) and LiteDB (iOS).
/// </summary>
public record DefenseRunProgress
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>Number of the wave the player must clear next (1-based).</summary>
    public int Wave { get; set; } = 1;

    /// <summary>Id of the map the run is played on (see <c>MapCatalog</c>). Defaults to the easiest map.</summary>
    public string MapId { get; set; } = MapCatalog.Default.Id;

    public int Lives { get; set; }

    public int Score { get; set; }

    /// <summary>JSON-serialized list of <see cref="PlacedTower"/> of the current layout.</summary>
    public string TowersJson { get; set; } = "[]";

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
