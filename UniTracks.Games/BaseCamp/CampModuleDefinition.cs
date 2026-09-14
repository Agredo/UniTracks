namespace UniTracks.Games.BaseCamp;

/// <summary>A purchasable camp module from the static <see cref="CampCatalog"/>.</summary>
public record CampModuleDefinition
{
    /// <summary>Stable identifier, e.g. "tent" — persisted on camp modules.</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>Fallback glyph for non-Skia UI (module list, upgrade buttons).</summary>
    public string Icon { get; init; } = "⛺";

    /// <summary>Highest reachable level; level 0 means "not built yet".</summary>
    public int MaxLevel { get; init; } = 1;

    /// <summary>Price of each level, index 0 = level 1. Always <see cref="MaxLevel"/> entries.</summary>
    public IReadOnlyList<CampCost> LevelCosts { get; init; } = Array.Empty<CampCost>();

    /// <summary>Gamification level needed to unlock this module (1 = always available).</summary>
    public int RequiredLevel { get; init; } = 1;

    /// <summary>Achievement id that must be unlocked first (prestige modules), or null.</summary>
    public string? RequiredAchievementId { get; init; }

    /// <summary>Price of reaching <paramref name="level"/>, or <see cref="CampCost.Free"/> when out of range.</summary>
    public CampCost CostFor(int level) =>
        level >= 1 && level <= LevelCosts.Count ? LevelCosts[level - 1] : CampCost.Free;
}
