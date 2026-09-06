using System.Text.Json;
using UniTracks.Games.Shared.Economy;
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
        var stats = await gameCatalogService.GetActivityStatsAsync();

        return new DefenseProfile
        {
            Coins = await gameCatalogService.GetCoinBalanceAsync(),
            Level = stats.Level,
            UnlockedAchievementIds = stats.UnlockedAchievementIds,
            UnlockedTowerIds = unlocks.Select(u => u.TowerId).ToList(),
            BestWave = record?.BestWave ?? 0,
            BestScore = record?.BestScore ?? 0,
            StartingEnergy = EnergyEconomy.ComputeStartingEnergy(stats),
            ClearBonus = EnergyEconomy.ComputeClearBonus(stats),
        };
    }

    public async Task<UnlockResult> TryUnlockAsync(string towerId)
    {
        var tower = TowerCatalog.Find(towerId);
        if (tower is null)
        {
            return UnlockResult.Fail("Unbekannter Turm.");
        }

        if (tower.IsFree)
        {
            return UnlockResult.Ok();
        }

        var stats = await gameCatalogService.GetActivityStatsAsync();

        if (tower.RequiredAchievementId is not null && !stats.UnlockedAchievementIds.Contains(tower.RequiredAchievementId))
        {
            return UnlockResult.Fail($"Erst die Errungenschaft „{AchievementName(tower.RequiredAchievementId)}“ freischalten.");
        }

        var unlocks = await store.LoadUnlocksAsync();
        if (unlocks.Any(u => u.TowerId == towerId))
        {
            return UnlockResult.Fail("Dieser Turm ist bereits freigeschaltet.");
        }

        if (stats.Level < tower.RequiredLevel)
        {
            return UnlockResult.Fail($"Erst Level {tower.RequiredLevel} erreichen (du bist Level {stats.Level}).");
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

    /// <summary>Human-readable achievement name for gate messages.</summary>
    private static string AchievementName(string id) => id switch
    {
        "hundred-km" => "100 km gesamt",
        "fifty-km" => "50 km gesamt",
        "marathon" => "Marathon-Bereit",
        "summit" => "Gipfelstürmer",
        _ => id,
    };

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

    public async Task<DefenseState?> LoadRunAsync()
    {
        var progress = await store.LoadRunAsync();
        if (progress is null)
        {
            return null;
        }

        var stats = await gameCatalogService.GetActivityStatsAsync();
        var unlocks = await store.LoadUnlocksAsync();
        var map = MapCatalog.Find(progress.MapId);

        var state = new DefenseState
        {
            UnlockedTowerIds = unlocks.Select(u => u.TowerId).ToList(),
            Map = map,
            Energy = EnergyEconomy.ComputeStartingEnergy(stats),
            ClearBonus = EnergyEconomy.ComputeClearBonus(stats),
            Lives = progress.Lives > 0 ? progress.Lives : map.StartLives,
            Score = progress.Score,
            NextWave = Math.Max(1, progress.Wave),
            Phase = DefensePhase.Building,
        };

        foreach (var tower in DeserializeTowers(progress.TowersJson))
        {
            state.Towers.Add(tower);
        }

        return state;
    }

    public async Task SaveRunAsync(DefenseState state)
    {
        var progress = new DefenseRunProgress
        {
            ID = Guid.NewGuid(),
            Wave = state.NextWave,
            MapId = state.Map.Id,
            Lives = state.Lives,
            Score = state.Score,
            TowersJson = SerializeTowers(state.Towers),
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await store.SaveRunAsync(progress);
    }

    public Task ClearRunAsync() => store.ClearRunAsync();

    private static string SerializeTowers(IEnumerable<PlacedTower> towers) =>
        JsonSerializer.Serialize(towers.Select(t => new SavedDefenseTower(t.X, t.Y, t.TowerId)));

    private static IEnumerable<PlacedTower> DeserializeTowers(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<PlacedTower>();
        }

        var saved = JsonSerializer.Deserialize<List<SavedDefenseTower>>(json) ?? new List<SavedDefenseTower>();
        return saved
            .Where(t => !string.IsNullOrWhiteSpace(t.TowerId))
            .Select(t => new PlacedTower { X = t.X, Y = t.Y, TowerId = t.TowerId });
    }

    /// <summary>Serialization DTO for a placed tower — avoids persisting runtime cooldown state.</summary>
    private sealed record SavedDefenseTower(int X, int Y, string TowerId);
}
