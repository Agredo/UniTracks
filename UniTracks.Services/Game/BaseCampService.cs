using UniTracks.Games.BaseCamp;
using UniTracks.Games.BaseCamp.Persistence;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Coordinates the pure <see cref="CampEngine"/> logic with persistence: every mutation is
/// validated first, persisted, and answered with a freshly rebuilt camp state, so the stock
/// and the shared coin balance always agree with what is actually stored.
/// </summary>
public class BaseCampService : IBaseCampService
{
    private readonly ICampStore campStore;
    private readonly IActivityStatsSource activityStats;
    private readonly ICoinAccountService coinAccount;

    public BaseCampService(ICampStore campStore, IActivityStatsSource activityStats, ICoinAccountService coinAccount)
    {
        this.campStore = campStore;
        this.activityStats = activityStats;
        this.coinAccount = coinAccount;
    }

    public async Task<CampState> GetCampAsync()
    {
        var modules = await campStore.LoadModulesAsync();
        var log = await EnsureLogAsync();
        var stats = await activityStats.GetAsync();

        // The account is shared with the other games, so their spending is gone here too.
        var account = await coinAccount.GetAsync();
        return CampEngine.Rebuild(modules, log, stats, DateTimeOffset.UtcNow, account.SpentOutsideCamp);
    }

    public async Task<CollectResult> CollectAsync()
    {
        var camp = await GetCampAsync();
        if (camp.Stock <= 0)
        {
            return CollectResult.Fail("Das Lager ist noch leer — beweg dich, dann füllt es sich.");
        }

        int collected = camp.Stock;
        var log = await campStore.LoadLogAsync();
        var now = DateTimeOffset.UtcNow;

        // Production is derived from the anchor, so a harvest only has to bank what is in the
        // camp and move the anchor forward by the hours that were paid out. Nothing about the
        // camp's fill level is written down.
        await campStore.SaveLogAsync(new CampLog
        {
            ID = log?.ID ?? Guid.NewGuid(),
            LastCollectedAt = CampEconomy.ComputeNextAnchor(log?.LastCollectedAt ?? now, now),
            TotalCollected = (log?.TotalCollected ?? 0) + collected,
            BankedSupplies = (log?.BankedSupplies ?? CampEconomy.StartingSupplies) + collected,
            UpdatedAt = now,
        });

        return CollectResult.Ok(await GetCampAsync(), collected);
    }

    public async Task<UpgradeResult> TryUpgradeAsync(string moduleId)
    {
        var camp = await GetCampAsync();
        var validation = CampEngine.ValidateUpgrade(camp, moduleId);
        if (!validation.Success)
        {
            return validation;
        }

        var modules = await campStore.LoadModulesAsync();
        var existing = modules.FirstOrDefault(m => m.ModuleId == moduleId);
        var now = DateTimeOffset.UtcNow;

        await campStore.SaveModuleAsync(new CampModule
        {
            ID = existing?.ID ?? Guid.NewGuid(),
            ModuleId = moduleId,
            Level = validation.NewLevel,
            PurchasedAt = existing?.PurchasedAt ?? now,
            UpdatedAt = now,
        });

        await ChargeSuppliesAsync(validation.Cost.Supplies, now);

        return validation;
    }

    /// <summary>
    /// Loads the camp's ledger, opening it on the first visit. The anchor is what production
    /// accrues from, so without this it would move along with every read and the camp would
    /// never start filling up — looking at the camp is what starts the clock.
    /// </summary>
    private async Task<CampLog> EnsureLogAsync()
    {
        var log = await campStore.LoadLogAsync();
        if (log is not null)
        {
            return log;
        }

        var now = DateTimeOffset.UtcNow;
        log = new CampLog
        {
            ID = Guid.NewGuid(),
            LastCollectedAt = now,
            TotalCollected = 0,
            BankedSupplies = CampEconomy.StartingSupplies,
            UpdatedAt = now,
        };

        await campStore.SaveLogAsync(log);
        return log;
    }

    /// <summary>
    /// Deducts the supply price of an upgrade. Coin prices need no equivalent: the coin balance
    /// is a pure function of the module levels, but the supply balance is not, so the price has
    /// to be recorded — otherwise every module would be free once the camp has been harvested.
    /// The harvest anchor is deliberately left alone: unharvested hours belong to the player.
    /// </summary>
    private async Task ChargeSuppliesAsync(int supplies, DateTimeOffset now)
    {
        if (supplies <= 0)
        {
            return;
        }

        var log = await campStore.LoadLogAsync();
        await campStore.SaveLogAsync(new CampLog
        {
            ID = log?.ID ?? Guid.NewGuid(),
            LastCollectedAt = log?.LastCollectedAt ?? now,
            TotalCollected = log?.TotalCollected ?? 0,
            BankedSupplies = Math.Max(0, (log?.BankedSupplies ?? CampEconomy.StartingSupplies) - supplies),
            UpdatedAt = now,
        });
    }
}
