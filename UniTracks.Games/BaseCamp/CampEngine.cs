using UniTracks.Games.BaseCamp.Persistence;
using UniTracks.Games.Shared.Economy;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Games.BaseCamp;

/// <summary>
/// Pure game logic for the base-camp idle game. No MAUI, SkiaSharp or EF dependencies —
/// takes persisted module levels plus the last harvest timestamp and produces the camp
/// state, and validates upgrade attempts.
/// </summary>
public static class CampEngine
{
    /// <summary>
    /// Rebuilds the full camp state from persisted modules, the harvest log and activity stats.
    /// </summary>
    /// <param name="modules">Persisted module levels (from the repository).</param>
    /// <param name="log">Persisted harvest log, or <c>null</c> on the very first visit.</param>
    /// <param name="stats">Lifetime activity (trips with type info and start times, XP, achievements, streak).</param>
    /// <param name="now">Reference time the stock is reconstructed for.</param>
    /// <param name="coinsSpentInOtherGames">
    /// Coins the player spent outside the camp (city buildings/expansions, tower unlocks and
    /// coin-funded energy). The account is shared, so the spendable balance subtracts them too.
    /// </param>
    public static CampState Rebuild(
        IEnumerable<CampModule> modules,
        CampLog? log,
        ActivityStats stats,
        DateTimeOffset now,
        int coinsSpentInOtherGames = 0)
    {
        var levels = ToLevels(modules);

        int earned = CoinEconomy.ComputeEarned(stats.Trips, stats.Xp, stats.UnlockedAchievements);
        int spent = ComputeSpent(levels) + coinsSpentInOtherGames;
        int capacity = CampEconomy.ComputeCapacity(levels);

        // A brand-new camp anchors on "now", so the welcome stock is all there is to spend.
        var anchor = log?.LastCollectedAt ?? now;
        double accrued = CampEconomy.ComputeAccrued(stats, levels, anchor, now);

        // The welcome stock is granted once, while the camp has never been touched. Handing it
        // out again would let the balance grow by tapping instead of by moving.
        int welcome = log is null ? CampEconomy.StartingSupplies : 0;

        // Two separate things: the supplies produced since the anchor, capped by the tent (what
        // the player can harvest), and the balance they own (what they can spend). Production is
        // capped, the balance is not — so a full camp stops producing, while spending or
        // harvesting frees it up again.
        int stock = Math.Min(capacity, (int)Math.Floor(accrued));
        int supplies = Math.Max(0, welcome + (log?.BankedSupplies ?? 0));

        return new CampState
        {
            Stock = stock,
            Supplies = supplies,
            Capacity = capacity,
            RatePerHour = CampEconomy.ComputeRate(stats, levels, now),
            WeightedWeeklyKm = CampEconomy.ComputeWeightedWeeklyKm(stats, levels, now),
            StreakFactor = CampEconomy.ComputeStreakFactor(stats, levels),
            Freshness = CampEconomy.ComputeFreshness(stats, levels, now),
            LastCollectedAt = anchor,
            TotalCollected = log?.TotalCollected ?? 0,
            Coins = Math.Max(0, earned - spent),
            CoinsEarned = earned,
            CoinsSpent = spent,
            Level = stats.Level,
            Xp = stats.Xp,
            UnlockedAchievementIds = stats.UnlockedAchievementIds,
            CurrentStreakDays = stats.CurrentStreakDays,
            BestStreakDays = stats.BestStreakDays,
            Modules = levels,
        };
    }

    /// <summary>
    /// Projects persisted module rows onto the level pairs the economy works with.
    /// </summary>
    public static IReadOnlyList<CampModuleLevel> ToLevels(IEnumerable<CampModule> modules) =>
        modules.Select(m => new CampModuleLevel { ModuleId = m.ModuleId, Level = m.Level }).ToList();

    /// <summary>Coins currently invested in camp modules (every purchased level counts).</summary>
    public static int ComputeSpent(IEnumerable<CampModuleLevel> modules) =>
        modules.Sum(m =>
        {
            var definition = CampCatalog.Find(m.ModuleId);
            if (definition is null)
            {
                return 0;
            }

            int coins = 0;
            for (int level = 1; level <= m.Level; level++)
            {
                coins += definition.CostFor(level).Coins;
            }

            return coins;
        });

    /// <summary>Validates a module upgrade request. Does not mutate anything.</summary>
    public static UpgradeResult ValidateUpgrade(CampState camp, string moduleId)
    {
        var definition = CampCatalog.Find(moduleId);
        if (definition is null)
        {
            return UpgradeResult.Fail("Unbekanntes Modul.");
        }

        int level = camp.LevelOf(moduleId);
        if (level >= definition.MaxLevel)
        {
            return UpgradeResult.Fail($"{definition.Name} ist schon voll ausgebaut.");
        }

        if (camp.Level < definition.RequiredLevel)
        {
            return UpgradeResult.Fail($"{definition.Name} braucht Level {definition.RequiredLevel} — bleib aktiv!");
        }

        if (definition.RequiredAchievementId is not null && !camp.UnlockedAchievementIds.Contains(definition.RequiredAchievementId))
        {
            return UpgradeResult.Fail($"{definition.Name} schaltest du durch einen Erfolg frei.");
        }

        var cost = definition.CostFor(level + 1);
        if (camp.Coins < cost.Coins)
        {
            return UpgradeResult.Fail("Nicht genug Coins — sammle mehr auf deinen Trips!");
        }

        if (camp.Supplies < cost.Supplies)
        {
            return UpgradeResult.Fail("Nicht genug Vorräte — das Lager füllt sich nur, wenn du aktiv bist.");
        }

        return UpgradeResult.Ok(moduleId, level + 1, cost);
    }
}
