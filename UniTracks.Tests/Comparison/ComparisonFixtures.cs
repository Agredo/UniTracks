using UniTracks.Models.Trip;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// Builds the small, exactly-known trips the comparison tests need. Everything is placed by hand so a
/// failure points at a rule and not at a GPS artefact.
/// </summary>
internal static class ComparisonFixtures
{
    private const double MetersPerDegreeLatitude = 111320.0;

    public static readonly DateTimeOffset DefaultStart = new(2026, 3, 1, 8, 0, 0, TimeSpan.FromHours(1));

    public static TripType Type(string identifier, string category) => new()
    {
        ID = Guid.NewGuid(),
        Name = identifier,
        Identifier = identifier,
        Description = identifier,
        Category = category,
    };

    /// <summary>A run northwards from the given corner. Longitude is kept constant for a clean line.</summary>
    public static List<LocationModel> StraightTrack(
        Guid tripId,
        double startLatitude,
        double startLongitude,
        double lengthMeters,
        double speedMetersPerSecond = 3.0,
        double spacingMeters = 10,
        DateTimeOffset? start = null)
    {
        var first = start ?? DefaultStart;
        int steps = Math.Max(1, (int)Math.Round(lengthMeters / spacingMeters));
        double step = lengthMeters / steps;

        var track = new List<LocationModel>(steps + 1);

        for (int i = 0; i <= steps; i++)
        {
            track.Add(new LocationModel
            {
                ID = Guid.NewGuid(),
                TripID = tripId,
                Latitude = startLatitude + i * step / MetersPerDegreeLatitude,
                Longitude = startLongitude,
                Altitude = 100,
                Accuracy = 5,
                Speed = speedMetersPerSecond,
                Timestamp = first.AddSeconds(i * step / speedMetersPerSecond),
            });
        }

        return track;
    }

    public static Trip Trip(
        Guid id,
        string name,
        IEnumerable<LocationModel> locations,
        TripType? type = null,
        double? distanceMeters = null,
        double? movingSeconds = null,
        DateTimeOffset? start = null)
    {
        var track = locations.ToList();
        var first = start ?? (track.Count > 0 ? track.Min(location => location.Timestamp) : DefaultStart);
        var last = track.Count > 0 ? track.Max(location => location.Timestamp) : first;

        return new Trip
        {
            ID = id,
            Name = name,
            StartTime = first,
            EndTime = last,
            Distance = distanceMeters,
            MovingTime = movingSeconds,
            TripTypeId = type?.ID,
            TripType = type,
            Locations = track,
        };
    }

    /// <summary>
    /// A trip whose numbers are fixed rather than derived, so a comparable-effort test isolates the
    /// matching rule from the effort model. The track only has to be long enough to be a route.
    /// </summary>
    public static Trip Fixed(
        Guid id,
        string name,
        TripType? type,
        double startLatitude,
        double startLongitude,
        double distanceMeters,
        double movingSeconds,
        DateTimeOffset? start = null)
    {
        var first = start ?? DefaultStart;
        var track = StraightTrack(id, startLatitude, startLongitude, distanceMeters, start: first);
        return Trip(id, name, track, type, distanceMeters, movingSeconds, first);
    }
}
