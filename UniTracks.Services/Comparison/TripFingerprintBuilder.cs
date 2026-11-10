using UniTracks.Models.Comparison;
using UniTracks.Models.Trip;
using UniTracks.Services.Location;
using UniTracks.Services.Stats;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Comparison;

/// <summary>
/// Derives a <see cref="TripFingerprint"/> from a trip's GPS track.
/// <para>
/// The numbers are taken the same way the trip detail page derives them (raw track for statistics,
/// smoothed track for shape) so the comparison never disagrees with the values the athlete already
/// sees on the trip itself.
/// </para>
/// </summary>
public static class TripFingerprintBuilder
{
    /// <summary>
    /// Bump whenever the derivation below changes, so stored fingerprints are rebuilt.
    /// 2: the fingerprint carries the trip's category and identifier.
    /// 3: raw-track totals and zero movement use the same rules as the comparison.
    /// </summary>
    public const int Version = 3;

    /// <summary>Points per resampled polyline; enough to keep the shape, few enough to compare cheaply.</summary>
    public const int PolylinePointCount = 32;

    /// <summary>Below this a track cannot describe a route.</summary>
    public const int MinimumPointCount = 2;

    /// <summary>Returns null when the trip has no usable track (manual entry, aborted recording).</summary>
    /// <param name="trip">The trip to fingerprint.</param>
    /// <param name="tripType">
    /// The trip's type, resolved from the catalogue. Supplied separately because the navigation is not
    /// loaded on every store, and the fingerprint has to carry the type's category and identifier.
    /// </param>
    /// <param name="smoothingEnabled">Mirrors the user's smoothing preference.</param>
    public static TripFingerprint? Build(Trip trip, TripType? tripType = null, bool smoothingEnabled = true)
    {
        var ordered = OrderedLocations(trip);
        if (ordered.Count < MinimumPointCount)
        {
            return null;
        }

        var smoothed = TrackSmoother.Smooth(ordered, smoothingEnabled);
        var shape = smoothed.Count >= MinimumPointCount ? smoothed : ordered;

        var geometry = new GeoPoint[shape.Count];
        for (int i = 0; i < shape.Count; i++)
        {
            geometry[i] = new GeoPoint(shape[i].Latitude, shape[i].Longitude);
        }

        var bounds = GeoMath.Bounds(geometry);
        var center = GeoMath.Centroid(geometry);
        var polyline = GeoMath.ResampleByDistance(geometry, PolylinePointCount);

        // The stored distance came from the raw track, just like the kilometre splits.
        double distance = TripMetrics.DistanceMeters(trip, ordered);

        double elevationGain = TripMetricsCalculator.ComputeElevationGain(ordered);

        double elapsed = TripMetrics.ElapsedSeconds(trip);
        double movingSeconds = TripMetrics.MovingSeconds(trip, ordered);

        var first = geometry[0];
        var last = geometry[^1];

        var type = tripType ?? trip.TripType;

        return new TripFingerprint
        {
            TripID = trip.ID,
            Version = Version,
            StartTime = trip.StartTime,
            TripTypeId = trip.TripTypeId,
            TripCategory = type?.Category ?? string.Empty,
            TripIdentifier = type?.Identifier ?? string.Empty,
            PointCount = ordered.Count,
            DistanceMeters = distance,
            ElapsedSeconds = elapsed,
            MovingSeconds = movingSeconds,
            AverageSpeedMetersPerSecond = TripMetrics.AverageSpeedMetersPerSecond(trip, ordered),
            MaxSpeedMetersPerSecond = trip.MaxSpeed ?? 0,
            ElevationGainMeters = elevationGain,
            MinAltitude = trip.MinAltitude ?? shape.Min(l => l.Altitude),
            MaxAltitude = trip.MaxAltitude ?? shape.Max(l => l.Altitude),
            MinLatitude = bounds.MinLatitude,
            MaxLatitude = bounds.MaxLatitude,
            MinLongitude = bounds.MinLongitude,
            MaxLongitude = bounds.MaxLongitude,
            CenterLatitude = center.Latitude,
            CenterLongitude = center.Longitude,
            StartCell = Geohash.Encode(first, Geohash.RoutePrecision),
            EndCell = Geohash.Encode(last, Geohash.RoutePrecision),
            StartArea = Geohash.Encode(first, Geohash.AreaPrecision),
            EndArea = Geohash.Encode(last, Geohash.AreaPrecision),
            RouteCells = Geohash.RouteCells(geometry),
            PolylineLatitudes = polyline.Select(p => p.Latitude).ToArray(),
            PolylineLongitudes = polyline.Select(p => p.Longitude).ToArray(),
            EffortScore = EffortModel.EffortScore(distance, movingSeconds, elapsed, elevationGain),
            EquivalentDistanceMeters = EffortModel.EquivalentDistanceMeters(distance, elevationGain),
        };
    }

    /// <summary>True when a stored fingerprint was made by an older derivation and must be rebuilt.</summary>
    public static bool IsStale(TripFingerprint fingerprint) => fingerprint.Version != Version;

    /// <summary>The fingerprint's resampled track as points, for the shape comparison.</summary>
    public static IReadOnlyList<GeoPoint> Polyline(TripFingerprint fingerprint)
    {
        int count = Math.Min(fingerprint.PolylineLatitudes.Length, fingerprint.PolylineLongitudes.Length);
        if (count == 0)
        {
            return Array.Empty<GeoPoint>();
        }

        var points = new GeoPoint[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = new GeoPoint(fingerprint.PolylineLatitudes[i], fingerprint.PolylineLongitudes[i]);
        }

        return points;
    }

    private static List<LocationModel> OrderedLocations(Trip trip)
    {
        if (trip.Locations is not { Count: > 0 })
        {
            return new List<LocationModel>();
        }

        return trip.Locations.OrderBy(l => l.Timestamp).ToList();
    }
}
