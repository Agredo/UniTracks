namespace UniTracks.Services.Location;

/// <summary>
/// Snapshot of the platform location capture. Used for diagnostics and for the UI warning when a
/// capture stops without any error.
/// </summary>
/// <param name="IsTracked">True when the platform reports capture details at all.</param>
/// <param name="IsRunning">True while the platform has been asked to deliver locations.</param>
/// <param name="Authorization">Human readable authorization as the platform reports it.</param>
/// <param name="FixCount">Number of fixes seen by the platform callback in this session.</param>
/// <param name="LastFixAt">Wall clock time of the last fix seen by the platform callback.</param>
/// <param name="LongestGapSeconds">Longest distance between two consecutive fix timestamps.</param>
public readonly record struct LocationCaptureHealth(
    bool IsTracked,
    bool IsRunning,
    string Authorization,
    int FixCount,
    DateTimeOffset? LastFixAt,
    double LongestGapSeconds)
{
    /// <summary>Unknown state, used by platforms that do not report capture details.</summary>
    public static LocationCaptureHealth Unknown { get; } = new(false, false, "unbekannt", 0, null, 0);

    /// <summary>Snapshot from a platform that reports capture details.</summary>
    public static LocationCaptureHealth Tracked(
        bool isRunning,
        string authorization,
        int fixCount,
        DateTimeOffset? lastFixAt,
        double longestGapSeconds)
        => new(true, isRunning, authorization, fixCount, lastFixAt, longestGapSeconds);
}
