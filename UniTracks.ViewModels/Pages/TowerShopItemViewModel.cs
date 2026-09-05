using UniTracks.Games.TowerDefense;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Shop entry wrapping a tower definition with its current unlock state,
/// so the UI can grey out coin-gated towers.
/// </summary>
public class TowerShopItemViewModel
{
    public TowerShopItemViewModel(TowerDefinition tower, DefenseProfile profile)
    {
        Tower = tower;
        IsUnlocked = tower.IsFree || profile.UnlockedTowerIds.Contains(tower.Id);
        IsAffordable = profile.Coins >= tower.UnlockCost;

        LockLabel = IsUnlocked ? string.Empty : $"🔒 {tower.UnlockCost:N0} 🪙";
    }

    public TowerDefinition Tower { get; }

    public string Id => Tower.Id;

    public string Icon => Tower.Icon;

    public string Name => Tower.Name;

    /// <summary>In-run energy price shown on unlocked towers.</summary>
    public int EnergyCost => Tower.EnergyCost;

    /// <summary>Permanently unlocked (starter tower or purchased with coins).</summary>
    public bool IsUnlocked { get; }

    /// <summary>Enough coins for the unlock purchase.</summary>
    public bool IsAffordable { get; }

    /// <summary>Unlock price shown on locked items ("" when unlocked).</summary>
    public string LockLabel { get; }
}
