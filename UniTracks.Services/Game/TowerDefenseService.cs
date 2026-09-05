using UniTracks.Games.TowerDefense;
using UniTracks.Games.TowerDefense.Persistence;

namespace UniTracks.Services.Game;

public class TowerDefenseService : ITowerDefenseService
{
    private readonly ITowerDefenseStore store;
    private readonly IGameCatalogService gameCatalogService;

    public TowerDefenseService(ITowerDefenseStore store, IGameCatalogService gameCatalogService)
    {
        this.store = store;
        this.gameCatalogService = gameCatalogService;
    }

    public async Task<DefenseProfile> GetProfileAsync()
    {
        var unlocks = await store.LoadUnlocksAsync();
        var record = await store.LoadRecordAsync();

        return new DefenseProfile
        {
            Coins = await gameCatalogService.GetCoinBalanceAsync(),
            UnlockedTowerIds = unlocks.Select(u => u.TowerId).ToList(),
            BestWave = record?.BestWave ?? 0,
            BestScore = record?.BestScore ?? 0,
        };
    }

    public async Task<UnlockResult> TryUnlockAsync(string towerId)
    {
        var tower = TowerCatalog.Find(towerId);
        if (tower is null)
        {
            return UnlockResult.Fail("Unbekannter Turm.");
        }

        var unlocks = await store.LoadUnlocksAsync();
        if (unlocks.Any(u => u.TowerId == towerId))
        {
            return UnlockResult.Fail("Dieser Turm ist bereits freigeschaltet.");
        }

        int coins = await gameCatalogService.GetCoinBalanceAsync();
        if (coins < tower.UnlockCost)
        {
            return UnlockResult.Fail($"Nicht genug Münzen — dir fehlen {tower.UnlockCost - coins:N0} 🪙.");
        }

        await store.SaveUnlockAsync(new TowerUnlock
        {
            ID = Guid.NewGuid(),
            TowerId = towerId,
            PurchasedAt = DateTimeOffset.UtcNow,
        });

        return UnlockResult.Ok();
    }

    public async Task<DefenseProfile> SaveRunResultAsync(int clearedWave, int score)
    {
        var record = await store.LoadRecordAsync();
        if (record is null)
        {
            record = new DefenseRecord { ID = Guid.NewGuid() };
        }

        if (clearedWave > record.BestWave || score > record.BestScore)
        {
            record.BestWave = Math.Max(record.BestWave, clearedWave);
            record.BestScore = Math.Max(record.BestScore, score);
            record.UpdatedAt = DateTimeOffset.UtcNow;
            await store.SaveRecordAsync(record);
        }

        return await GetProfileAsync();
    }
}
