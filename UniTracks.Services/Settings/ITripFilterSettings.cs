namespace UniTracks.Services.Settings;

/// <summary>
/// The trip filter the user last picked, kept as plain strings so the ViewModel layer stays free of
/// platform types. Restoring it means the trip list opens the way it was left.
/// </summary>
public interface ITripFilterSettings
{
    /// <summary>Selected trip type ids, semicolon separated. Empty means "every type".</summary>
    string TypeIds { get; set; }

    /// <summary>Name of the chosen <c>DateRangePreset</c>, <c>"All"</c> for "no date filter".</summary>
    string DateRange { get; set; }

    /// <summary>Selected duration bucket names, semicolon separated.</summary>
    string Durations { get; set; }

    /// <summary>Selected distance bucket names, semicolon separated.</summary>
    string Distances { get; set; }
}
