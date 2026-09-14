using UniTracks.Games.BaseCamp;

namespace UniTracks.Services.Game;

/// <summary>
/// Use-case service for the base-camp idle game: load the camp, harvest the accrued
/// supplies and upgrade modules.
/// </summary>
public interface IBaseCampService
{
    /// <summary>
    /// Rebuilds the current camp state (stock, rate, capacity, coin balance) from persistence
    /// and the player's real activity.
    /// </summary>
    Task<CampState> GetCampAsync();

    /// <summary>
    /// Banks the supplies that accrued since the last harvest and moves the anchor forward by
    /// the completed hours, so the camp starts filling up again while the leftover partial hour
    /// is kept. Fails when the camp is empty.
    /// </summary>
    Task<CollectResult> CollectAsync();

    /// <summary>
    /// Upgrades a module one level and charges its price. Fails (no persistence) when invalid
    /// or unaffordable.
    /// </summary>
    Task<UpgradeResult> TryUpgradeAsync(string moduleId);
}
