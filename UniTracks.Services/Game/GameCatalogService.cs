using UniTracks.Games.Catalog;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Game registry plus the shared coin balance. The balance itself comes from
/// <see cref="ICoinAccountService"/>, so the catalog and both games always agree.
/// </summary>
public class GameCatalogService : IGameCatalogService
{
    private readonly ICoinAccountService coinAccount;
    private readonly IActivityStatsSource activityStats;

    public GameCatalogService(ICoinAccountService coinAccount, IActivityStatsSource activityStats)
    {
        this.coinAccount = coinAccount;
        this.activityStats = activityStats;
    }

    public IReadOnlyList<GameInfo> GetGames() => GameCatalog.Games;

    public async Task<ActivityStats> GetActivityStatsAsync() => await activityStats.GetAsync();

    public async Task<int> GetCoinBalanceAsync() => (await coinAccount.GetAsync()).Balance;
}
