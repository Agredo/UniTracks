using UniTracks.Models.Trip;

namespace UniTracks.Services.Comparison;

/// <summary>
/// The name shown for a trip. Trips recorded without a name get a time-of-day label, which is the
/// convention the trip list and the trip detail page already use.
/// </summary>
public static class TripDisplay
{
    public static string Name(Trip trip)
    {
        if (!string.IsNullOrWhiteSpace(trip.Name))
        {
            return trip.Name;
        }

        return TimeOfDayName(trip.StartTime);
    }

    public static string TimeOfDayName(DateTimeOffset startTime) => startTime.Hour switch
    {
        >= 5 and < 11 => "Morgen Trip",
        >= 11 and < 14 => "Mittags Trip",
        >= 14 and < 18 => "Nachmittags Trip",
        _ => "Abend Trip",
    };
}
