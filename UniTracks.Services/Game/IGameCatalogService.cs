using UniTracks.Games.Catalog;

namespace UniTracks.Services.Game;

/// <summary>Provides the extensible list of available mini games plus the current coin balance.</summary>
public interface IGameCatalogService
{
    IReadOnlyList<GameInfo> GetGames();

    Task<int> GetCoinBalanceAsync();
}
