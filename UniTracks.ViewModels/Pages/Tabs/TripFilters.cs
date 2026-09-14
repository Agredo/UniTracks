using UniTracks.Models.Trip;
using UniTracks.Services.Settings;

namespace UniTracks.ViewModels.Pages.Tabs;

/// <summary>
/// The date windows offered by the trip filter. A single one is chosen at a time; <see cref="All"/>
/// means "no date filter". The names are stored in the preferences, so they must stay stable.
/// </summary>
public enum DateRangePreset
{
    All,
    Last7Days,
    Last30Days,
    ThisYear,
}

/// <summary>Duration buckets offered by the trip filter. Several can be chosen at once (OR).</summary>
public enum DurationBucket
{
    Under30Minutes,
    Between30And60Minutes,
    Over60Minutes,
}

/// <summary>Distance buckets offered by the trip filter. Several can be chosen at once (OR).</summary>
public enum DistanceBucket
{
    Under5Kilometers,
    From5To10Kilometers,
    From10To21Kilometers,
    HalfMarathonAndMore,
}

/// <summary>
/// The trip filter: which types, one date window, duration buckets and distance buckets.
/// Values inside a group are OR-ed (Laufen <em>oder</em> Radfahren), the groups themselves are
/// AND-ed (Typ <em>und</em> Zeitraum) — the behaviour users know from photo, mail and streaming apps.
/// Immutable, so every change produces a new filter and nothing can be mutated behind a view's back.
/// </summary>
public sealed record TripFilters
{
    /// <summary>Half marathon in metres; the top distance bucket starts above it.</summary>
    private const double HalfMarathonMeters = 21097.5;

    /// <summary>The filter that keeps every trip.</summary>
    public static TripFilters Empty { get; } = new();

    /// <summary>Selected trip type ids. Empty means "every type".</summary>
    public IReadOnlyCollection<Guid> TypeIds { get; init; } = Array.Empty<Guid>();

    /// <summary>The chosen date window, <see cref="DateRangePreset.All"/> for "no date filter".</summary>
    public DateRangePreset DateRange { get; init; } = DateRangePreset.All;

    /// <summary>Selected duration buckets. Empty means "every duration".</summary>
    public IReadOnlyCollection<DurationBucket> Durations { get; init; } = Array.Empty<DurationBucket>();

    /// <summary>Selected distance buckets. Empty means "every distance".</summary>
    public IReadOnlyCollection<DistanceBucket> Distances { get; init; } = Array.Empty<DistanceBucket>();

    /// <summary>
    /// How many filter groups are in use (0..4). This is what the badge on the filter button shows —
    /// the count of active filters, not the count of matching trips.
    /// </summary>
    public int ActiveCount =>
        (TypeIds.Count > 0 ? 1 : 0)
        + (DateRange != DateRangePreset.All ? 1 : 0)
        + (Durations.Count > 0 ? 1 : 0)
        + (Distances.Count > 0 ? 1 : 0);

    /// <summary>True when at least one group narrows the list.</summary>
    public bool HasAny => ActiveCount > 0;

    /// <summary>Whether <paramref name="trip"/> passes every active group.</summary>
    public bool Matches(Trip trip) => Matches(trip, DateTimeOffset.Now);

    /// <summary>
    /// Whether <paramref name="trip"/> passes every active group, judged against <paramref name="now"/>.
    /// The date is passed in so the relative windows (last 7/30 days, this year) are testable.
    /// </summary>
    public bool Matches(Trip trip, DateTimeOffset now)
    {
        if (TypeIds.Count > 0 && (trip.TripTypeId is not { } typeId || !TypeIds.Contains(typeId)))
        {
            return false;
        }

        if (!MatchesDate(trip.StartTime, now))
        {
            return false;
        }

        if (Durations.Count > 0 && !Durations.Contains(BucketFor(trip.EndTime - trip.StartTime)))
        {
            return false;
        }

        if (Distances.Count > 0 && !Distances.Contains(BucketFor(trip.Distance)))
        {
            return false;
        }

        return true;
    }

    /// <summary>Adds the type when it is missing, removes it when it is selected.</summary>
    public TripFilters ToggleType(Guid typeId) => this with
    {
        TypeIds = TypeIds.Contains(typeId)
            ? TypeIds.Where(id => id != typeId).ToList()
            : TypeIds.Append(typeId).ToList(),
    };

    public TripFilters SetDateRange(DateRangePreset preset) => this with { DateRange = preset };

    public TripFilters ToggleDuration(DurationBucket bucket) => this with
    {
        Durations = Durations.Contains(bucket)
            ? Durations.Where(value => value != bucket).ToList()
            : Durations.Append(bucket).ToList(),
    };

    public TripFilters ToggleDistance(DistanceBucket bucket) => this with
    {
        Distances = Distances.Contains(bucket)
            ? Distances.Where(value => value != bucket).ToList()
            : Distances.Append(bucket).ToList(),
    };

    /// <summary>Drops every filter.</summary>
    public TripFilters Clear() => Empty;

    /// <summary>
    /// Which duration bucket a recorded span falls in. The duration is computed from
    /// <c>EndTime - StartTime</c> because <c>TotalTime</c>/<c>MovingTime</c>/<c>StoppedTime</c> are
    /// never written by the app — the same way the trip card shows it.
    /// </summary>
    public static DurationBucket BucketFor(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMinutes(30))
        {
            return DurationBucket.Under30Minutes;
        }

        return duration <= TimeSpan.FromMinutes(60)
            ? DurationBucket.Between30And60Minutes
            : DurationBucket.Over60Minutes;
    }

    /// <summary>Which distance bucket a trip falls in. <paramref name="distance"/> is in metres.</summary>
    public static DistanceBucket BucketFor(double? distance) => (distance ?? 0) switch
    {
        < 5000 => DistanceBucket.Under5Kilometers,
        <= 10000 => DistanceBucket.From5To10Kilometers,
        <= HalfMarathonMeters => DistanceBucket.From10To21Kilometers,
        _ => DistanceBucket.HalfMarathonAndMore,
    };

    private bool MatchesDate(DateTimeOffset startTime, DateTimeOffset now)
    {
        // Compared on whole days so "letzte 7 Tage" does not depend on the time of day the filter
        // is opened at.
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);

        return DateRange switch
        {
            DateRangePreset.Last7Days => startTime >= today.AddDays(-7),
            DateRangePreset.Last30Days => startTime >= today.AddDays(-30),
            DateRangePreset.ThisYear => startTime.Year == now.Year,
            _ => true,
        };
    }
}

/// <summary>
/// Reads and writes a <see cref="TripFilters"/> through <see cref="ITripFilterSettings"/>. Unknown or
/// malformed values are dropped rather than throwing, so a filter written by an older version just
/// falls back to "not selected".
/// </summary>
public static class TripFilterStorage
{
    private const char Separator = ';';

    public static TripFilters Read(ITripFilterSettings settings) => new()
    {
        TypeIds = ReadGuids(settings.TypeIds),
        DateRange = ReadEnum(settings.DateRange, DateRangePreset.All),
        Durations = ReadEnums<DurationBucket>(settings.Durations),
        Distances = ReadEnums<DistanceBucket>(settings.Distances),
    };

    public static void Write(ITripFilterSettings settings, TripFilters filters)
    {
        settings.TypeIds = string.Join(Separator, filters.TypeIds);
        settings.DateRange = filters.DateRange.ToString();
        settings.Durations = string.Join(Separator, filters.Durations);
        settings.Distances = string.Join(Separator, filters.Distances);
    }

    private static IReadOnlyCollection<Guid> ReadGuids(string value) => Split(value)
        .Select(part => Guid.TryParse(part, out var id) ? id : (Guid?)null)
        .Where(id => id is not null)
        .Select(id => id!.Value)
        .ToList();

    private static IReadOnlyCollection<TEnum> ReadEnums<TEnum>(string value)
        where TEnum : struct, Enum => Split(value)
        .Select(part => Enum.TryParse<TEnum>(part, out var parsed) ? parsed : (TEnum?)null)
        .Where(parsed => parsed is not null)
        .Select(parsed => parsed!.Value)
        .ToList();

    private static TEnum ReadEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct, Enum => Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;

    private static IEnumerable<string> Split(string value) => value
        .Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>German labels for the filter values, so the sheet and the test read the same wording.</summary>
public static class TripFilterLabels
{
    public static string For(DateRangePreset preset) => preset switch
    {
        DateRangePreset.Last7Days => "Letzte 7 Tage",
        DateRangePreset.Last30Days => "Letzte 30 Tage",
        DateRangePreset.ThisYear => "Dieses Jahr",
        _ => "Gesamt",
    };

    public static string For(DurationBucket bucket) => bucket switch
    {
        DurationBucket.Under30Minutes => "unter 30 min",
        DurationBucket.Between30And60Minutes => "30–60 min",
        _ => "über 60 min",
    };

    public static string For(DistanceBucket bucket) => bucket switch
    {
        DistanceBucket.Under5Kilometers => "unter 5 km",
        DistanceBucket.From5To10Kilometers => "5–10 km",
        DistanceBucket.From10To21Kilometers => "10–21 km",
        _ => "Halbmarathon+",
    };
}
