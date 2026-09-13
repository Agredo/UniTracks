namespace UniTracks.Services.Settings;

/// <summary>
/// User-facing switch that turns the GPS post-processing
/// (<see cref="UniTracks.Services.Location.TrackSmoother"/>) off for the map view. It only affects the
/// rendered route: distances keep being calculated from the smoothed track, so the statistics stay
/// comparable no matter which setting is active. Default is on.
/// </summary>
public interface ITrackSmoothingSettings
{
    /// <summary>
    /// <c>true</c>: the map draws the filtered/moving-averaged track. <c>false</c>: it draws the raw
    /// recorded points — useful to see what the smoothing removes (e.g. tight turnarounds).
    /// </summary>
    bool IsEnabled { get; set; }
}
