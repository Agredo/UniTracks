namespace UniTracks.Models.Trip;

/// <summary>
/// Decides whether a trip counts for gamification (achievements, XP, streaks, coins).
/// Trips that are too short in time AND distance (e.g. accidentally started recordings
/// or cheating attempts) are excluded so achievements cannot be unlocked with
/// 10-second / 10-meter trips.
/// </summary>
public static class TripQualification
{
    /// <summary>Minimum duration a trip must last to count.</summary>
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(2);

    /// <summary>Minimum distance in meters a trip must cover to count.</summary>
    public const double MinDistanceMeters = 100;

    public static bool IsQualifying(Trip trip)
    {
        var duration = trip.EndTime - trip.StartTime;
        return duration >= MinDuration && (trip.Distance ?? 0) >= MinDistanceMeters;
    }
}
