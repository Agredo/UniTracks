using UniTracks.Games.Catalog;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Services.Game;

/// <summary>Provides the extensible list of available mini games plus the current coin balance.</summary>
public interface IGameCatalogService
{
    IReadOnlyList<GameInfo> GetGames();

    Task<int> GetCoinBalanceAsync();

    /// <summary>Lifetime activity stats — drives level/achievement gates on game unlocks.</summary>
    Task<ActivityStats> GetActivityStatsAsync();
}
