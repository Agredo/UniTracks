using UniTracks.Services.Settings;

namespace UniTracks.Maui.Services.Settings;

/// <inheritdoc />
public sealed class PreferencesTripFilterSettings : ITripFilterSettings
{
    private const string TypeIdsKey = "UniTracks.Settings.TripFilters.TypeIds";

    private const string DateRangeKey = "UniTracks.Settings.TripFilters.DateRange";

    private const string DurationsKey = "UniTracks.Settings.TripFilters.Durations";

    private const string DistancesKey = "UniTracks.Settings.TripFilters.Distances";

    /// <summary>Empty by default: the list showed every trip before the filter existed.</summary>
    public string TypeIds
    {
        get => Preferences.Default.Get(TypeIdsKey, string.Empty);
        set => Preferences.Default.Set(TypeIdsKey, value);
    }

    public string DateRange
    {
        get => Preferences.Default.Get(DateRangeKey, "All");
        set => Preferences.Default.Set(DateRangeKey, value);
    }

    public string Durations
    {
        get => Preferences.Default.Get(DurationsKey, string.Empty);
        set => Preferences.Default.Set(DurationsKey, value);
    }

    public string Distances
    {
        get => Preferences.Default.Get(DistancesKey, string.Empty);
        set => Preferences.Default.Set(DistancesKey, value);
    }
}
