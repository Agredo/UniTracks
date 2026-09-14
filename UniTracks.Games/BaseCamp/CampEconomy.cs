using UniTracks.Games.Shared.Economy;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Games.BaseCamp;

/// <summary>
/// Economy rules for the base-camp idle game. Supplies are the camp's own, non-tradable
/// resource: they accumulate over time, but only as fast as the player's real activity
/// allows, and they decay when the player stops moving. Coins stay exactly what they are
/// everywhere else — earned from trips, level-ups and achievements, spent across all games —
/// so the camp never mints or burns coins.
/// </summary>
public static class CampEconomy
{
    /// <summary>Welcome stock, so the first module is affordable right away.</summary>
    public const int StartingSupplies = 250;

    /// <summary>Supplies a brand-new camp holds at once.</summary>
    public const int BaseCapacity = 400;

    /// <summary>Capacity multiplier per tent level.</summary>
    public const double TentCapacityGrowth = 1.5;

    /// <summary>Supplies per hour with no activity at all and no kitchen.</summary>
    public const double BaseRatePerHour = 6.0;

    /// <summary>Base-rate multiplier per kitchen level.</summary>
    public const double KitchenRateGrowth = 1.25;

    /// <summary>Rolling window of real activity that feeds the production rate.</summary>
    public const int ActivityWindowDays = 7;

    /// <summary>Extra supplies per hour for each weighted kilometre of the last week.</summary>
    public const double RatePerWeightedKm = 0.8;

    /// <summary>Extra weekly-kilometre weight per map level.</summary>
    public const double MapKmBonusPerLevel = 0.20;

    /// <summary>Streak multiplier gained per consecutive active day.</summary>
    public const double StreakBonusPerDay = 0.10;

    /// <summary>Streak days that still increase the multiplier.</summary>
    public const int StreakBonusCapDays = 10;

    /// <summary>Extra streak multiplier per radio level.</summary>
    public const double RadioStreakBonusPerLevel = 0.05;

    /// <summary>Rate and capacity bonus once the hut is built.</summary>
    public const double HutBonus = 0.10;

    /// <summary>Hours after the last trip during which production runs at full speed.</summary>
    public const double FreshnessFullHours = 24;

    /// <summary>
    /// Longest window that is ever paid out in one go. Older time is lost — the camp was
    /// full anyway, and a hard bound keeps the hourly reconstruction cheap.
    /// </summary>
    public const int MaxCatchUpHours = 48;

    /// <summary>Production factor per full day without activity (last entry = floor).</summary>
    private static readonly double[] FreshnessDecay = { 1.0, 0.6, 0.35, 0.2, 0.12 };

    /// <summary>Lowest production factor per cellar level.</summary>
    private static readonly double[] CellarFloors = { 0.08, 0.15, 0.25, 0.40 };

    /// <summary>Level of one module (0 when it was never built).</summary>
    public static int LevelOf(IReadOnlyList<CampModuleLevel> modules, string moduleId) =>
        modules.FirstOrDefault(m => m.ModuleId == moduleId)?.Level ?? 0;

    /// <summary>Supplies the camp holds at once — everything beyond that is lost.</summary>
    public static int ComputeCapacity(IReadOnlyList<CampModuleLevel> modules)
    {
        double capacity = BaseCapacity * Math.Pow(TentCapacityGrowth, Math.Max(0, LevelOf(modules, CampCatalog.TentId)));
        if (LevelOf(modules, CampCatalog.HutId) > 0)
        {
            capacity *= 1 + HutBonus;
        }

        return (int)Math.Round(capacity);
    }

    /// <summary>Lowest production factor the camp can drop to, raised by the cellar.</summary>
    public static double ComputeFreshnessFloor(IReadOnlyList<CampModuleLevel> modules)
    {
        int cellar = Math.Clamp(LevelOf(modules, CampCatalog.CellarId), 0, CellarFloors.Length - 1);
        return CellarFloors[cellar];
    }

    /// <summary>
    /// Production factor at <paramref name="at"/>, driven by how long ago the player last
    /// moved. A player without any recorded trip starts at full speed, otherwise the very
    /// first session would already be throttled.
    /// </summary>
    public static double ComputeFreshness(ActivityStats stats, IReadOnlyList<CampModuleLevel> modules, DateTimeOffset at)
    {
        var lastTrip = LastTripAt(stats, at);
        if (lastTrip is null)
        {
            return 1.0;
        }

        double hours = Math.Max(0, (at - lastTrip.Value).TotalHours);
        int step = Math.Min((int)(hours / FreshnessFullHours), FreshnessDecay.Length - 1);
        return Math.Max(FreshnessDecay[step], ComputeFreshnessFloor(modules));
    }

    /// <summary>
    /// Weighted kilometres of the rolling week ending at <paramref name="at"/>, scaled by the
    /// same per-trip-type effort factors the coin economy uses.
    /// </summary>
    public static double ComputeWeightedWeeklyKm(ActivityStats stats, IReadOnlyList<CampModuleLevel> modules, DateTimeOffset at)
    {
        var since = at.AddDays(-ActivityWindowDays);
        double km = 0;
        foreach (var trip in stats.Trips)
        {
            if (trip.StartedAt == default || trip.StartedAt <= since || trip.StartedAt > at)
            {
                continue;
            }

            km += trip.DistanceKm * TripTypeFactors.For(trip.Category, trip.Identifier);
        }

        double bonus = 1 + MapKmBonusPerLevel * LevelOf(modules, CampCatalog.MapId);
        return km * bonus;
    }

    /// <summary>Multiplier from the current streak (capped) plus the radio bonus.</summary>
    public static double ComputeStreakFactor(ActivityStats stats, IReadOnlyList<CampModuleLevel> modules)
    {
        int days = Math.Min(Math.Max(0, stats.CurrentStreakDays), StreakBonusCapDays);
        return 1 + StreakBonusPerDay * days + RadioStreakBonusPerLevel * LevelOf(modules, CampCatalog.RadioId);
    }

    /// <summary>Supplies produced per hour at <paramref name="at"/>.</summary>
    public static double ComputeRate(ActivityStats stats, IReadOnlyList<CampModuleLevel> modules, DateTimeOffset at)
    {
        double baseRate = BaseRatePerHour * Math.Pow(KitchenRateGrowth, Math.Max(0, LevelOf(modules, CampCatalog.KitchenId)));
        double activityRate = RatePerWeightedKm * ComputeWeightedWeeklyKm(stats, modules, at);

        double rate = (baseRate + activityRate)
            * ComputeStreakFactor(stats, modules)
            * ComputeFreshness(stats, modules, at);

        if (LevelOf(modules, CampCatalog.HutId) > 0)
        {
            rate *= 1 + HutBonus;
        }

        return rate;
    }

    /// <summary>
    /// Supplies accrued between <paramref name="lastCollectedAt"/> and <paramref name="now"/>.
    /// Nothing is stored, so this reconstructs the rate hour by hour: the rate only changes
    /// when the rolling week, the freshness window or the streak crosses a boundary.
    /// </summary>
    public static double ComputeAccrued(
        ActivityStats stats,
        IReadOnlyList<CampModuleLevel> modules,
        DateTimeOffset lastCollectedAt,
        DateTimeOffset now)
    {
        int hours = ComputeWholeHours(lastCollectedAt, now);
        if (hours == 0)
        {
            return 0;
        }

        double capacity = ComputeCapacity(modules);
        double total = 0;
        for (int i = 0; i < hours && total < capacity; i++)
        {
            total += ComputeRate(stats, modules, StartOfWindow(lastCollectedAt, now).AddHours(i));
        }

        return Math.Min(total, capacity);
    }

    /// <summary>
    /// Completed hours between the last harvest and <paramref name="now"/>. Partial hours only
    /// pay out once they are full, so repeatedly tapping "collect" cannot mint supplies.
    /// </summary>
    public static int ComputeWholeHours(DateTimeOffset lastCollectedAt, DateTimeOffset now)
    {
        var start = StartOfWindow(lastCollectedAt, now);
        return start >= now ? 0 : (int)Math.Floor((now - start).TotalHours);
    }

    /// <summary>
    /// Anchor the next harvest accrues from: it advances by the whole hours that were paid out,
    /// so the leftover partial hour is kept instead of being dropped.
    /// </summary>
    public static DateTimeOffset ComputeNextAnchor(DateTimeOffset lastCollectedAt, DateTimeOffset now) =>
        StartOfWindow(lastCollectedAt, now).AddHours(ComputeWholeHours(lastCollectedAt, now));

    /// <summary>
    /// Beginning of the pay-out window: the anchor, or the start of the catch-up limit when the
    /// player was away longer than <see cref="MaxCatchUpHours"/> (older time is lost).
    /// </summary>
    private static DateTimeOffset StartOfWindow(DateTimeOffset lastCollectedAt, DateTimeOffset now)
    {
        var earliest = now.AddHours(-MaxCatchUpHours);
        return lastCollectedAt < earliest ? earliest : lastCollectedAt;
    }

    /// <summary>Most recent trip at or before <paramref name="at"/> (null without any history).</summary>
    private static DateTimeOffset? LastTripAt(ActivityStats stats, DateTimeOffset at)
    {
        DateTimeOffset? last = null;
        foreach (var trip in stats.Trips)
        {
            if (trip.StartedAt == default || trip.StartedAt > at)
            {
                continue;
            }

            if (last is null || trip.StartedAt > last)
            {
                last = trip.StartedAt;
            }
        }

        return last;
    }
}
