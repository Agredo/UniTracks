using UniTracks.Games.CityBuilder;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Shop entry wrapping a building definition with its current unlock state,
/// so the UI can grey out level-/achievement-gated buildings.
/// </summary>
public class ShopItemViewModel
{
    public ShopItemViewModel(BuildingDefinition building, CityState city)
    {
        Building = building;
        IsUnlocked = city.IsUnlocked(building);
        IsAffordable = city.Coins >= building.Cost;

        LockLabel = building.RequiredAchievementId is not null && !city.UnlockedAchievementIds.Contains(building.RequiredAchievementId)
            ? "🏅 Erfolg nötig"
            : city.Level < building.RequiredLevel
                ? $"🔒 Level {building.RequiredLevel}"
                : string.Empty;
    }

    public BuildingDefinition Building { get; }

    public string Id => Building.Id;

    public string Icon => Building.Icon;

    public string Name => Building.Name;

    public int Cost => Building.Cost;

    /// <summary>Level + achievement gates satisfied.</summary>
    public bool IsUnlocked { get; }

    /// <summary>Enough coins for the current balance.</summary>
    public bool IsAffordable { get; }

    /// <summary>Reason shown on locked items ("" when unlocked).</summary>
    public string LockLabel { get; }
}
