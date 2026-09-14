using System.ComponentModel.DataAnnotations;

namespace UniTracks.Games.BaseCamp.Persistence;

/// <summary>
/// A built camp module with its current level (single row per module). Supplies are always
/// computed from the harvest log plus real activity, so levels are the only persisted
/// module state. Works on EF Core (SQLite) as well as LiteDB on iOS.
/// </summary>
public record CampModule
{
    [Key]
    public Guid ID { get; init; }

    /// <summary>References <c>CampModuleDefinition.Id</c> from the static catalog.</summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>Level reached so far (1 = built once).</summary>
    public int Level { get; set; }

    public DateTimeOffset PurchasedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
