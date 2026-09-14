using UniTracks.Games.BaseCamp;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Shop entry wrapping a camp module with its current level, unlock state and next price,
/// so the UI can grey out level-/achievement-gated and unaffordable upgrades.
/// </summary>
public class BaseCampShopItemViewModel
{
    public BaseCampShopItemViewModel(CampModuleDefinition module, CampState camp)
    {
        Module = module;
        Level = camp.LevelOf(module.Id);
        IsMaxed = camp.IsMaxed(module);
        IsUnlocked = camp.IsUnlocked(module);
        NextCost = camp.NextCost(module);

        IsAffordable = !IsMaxed && IsUnlocked
            && camp.Coins >= NextCost.Coins
            && camp.Supplies >= NextCost.Supplies;

        LockLabel = module.RequiredAchievementId is not null && !camp.UnlockedAchievementIds.Contains(module.RequiredAchievementId)
            ? "🏅 Erfolg nötig"
            : camp.Level < module.RequiredLevel
                ? $"🔒 Level {module.RequiredLevel}"
                : string.Empty;

        LevelLabel = IsMaxed ? "Max" : $"Lv {Level}/{module.MaxLevel}";
    }

    public CampModuleDefinition Module { get; }

    public string Id => Module.Id;

    public string Icon => Module.Icon;

    public string Name => Module.Name;

    public string Description => Module.Description;

    /// <summary>Level the module is built at right now (0 = not built yet).</summary>
    public int Level { get; }

    /// <summary>"Lv 1/4" or "Max".</summary>
    public string LevelLabel { get; }

    /// <summary>Price of the next level ("—" when maxed out).</summary>
    public CampCost NextCost { get; }

    public string CostLabel => IsMaxed ? "—" : FormatCost(NextCost);

    /// <summary>Level + achievement gates satisfied.</summary>
    public bool IsUnlocked { get; }

    /// <summary>No further level remains.</summary>
    public bool IsMaxed { get; }

    /// <summary>Unlocked, not maxed and both currencies suffice.</summary>
    public bool IsAffordable { get; }

    /// <summary>Reason shown on locked items ("" when unlocked).</summary>
    public string LockLabel { get; }

    private static string FormatCost(CampCost cost)
    {
        if (cost.IsFree)
        {
            return "gratis";
        }

        var parts = new List<string>();
        if (cost.Supplies > 0)
        {
            parts.Add($"{cost.Supplies:N0} 📦");
        }

        if (cost.Coins > 0)
        {
            parts.Add($"{cost.Coins:N0} 🪙");
        }

        return string.Join(" · ", parts);
    }
}
