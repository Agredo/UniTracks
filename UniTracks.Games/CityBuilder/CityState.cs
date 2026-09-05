namespace UniTracks.Games.CityBuilder;

/// <summary>Complete state of the player's city: grid tiles plus the current coin balance.</summary>
public record CityState
{
    public const int DefaultGridSize = 6;

    public int GridSize { get; init; } = DefaultGridSize;

    /// <summary>All tiles, row-major (Y * GridSize + X). Always GridSize² entries.</summary>
    public IReadOnlyList<CityTile> Tiles { get; init; } = Array.Empty<CityTile>();

    /// <summary>Spendable coins (earned − spent).</summary>
    public int Coins { get; init; }

    /// <summary>Total coins ever earned through trips and achievements.</summary>
    public int CoinsEarned { get; init; }

    /// <summary>Total coins spent on buildings and city expansions.</summary>
    public int CoinsSpent { get; init; }

    /// <summary>Gamification level — gates buildings and expansions.</summary>
    public int Level { get; init; } = 1;

    /// <summary>Gamification XP.</summary>
    public int Xp { get; init; }

    /// <summary>Ids of unlocked achievements — gate prestige buildings.</summary>
    public IReadOnlyList<string> UnlockedAchievementIds { get; init; } = Array.Empty<string>();

    /// <summary>Next purchasable expansion step, or null when maxed out.</summary>
    public CityExpansionStep? NextExpansion => CityExpansions.NextStep(GridSize);

    /// <summary>True when the player can buy the next expansion right now (level + coins).</summary>
    public bool CanExpand =>
        NextExpansion is { } step && Level >= step.RequiredLevel && Coins >= step.Cost;

    /// <summary>True when the building is unlocked for this player (level + achievement gates).</summary>
    public bool IsUnlocked(BuildingDefinition building) =>
        Level >= building.RequiredLevel
        && (building.RequiredAchievementId is null || UnlockedAchievementIds.Contains(building.RequiredAchievementId));

    public CityTile? GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= GridSize || y >= GridSize)
        {
            return null;
        }

        return Tiles.Count == GridSize * GridSize ? Tiles[y * GridSize + x] : null;
    }

    public int BuildingCount => Tiles.Count(t => !t.IsEmpty);
}
