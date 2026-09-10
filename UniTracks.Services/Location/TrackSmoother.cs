using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Location;

/// <summary>
/// Post-processing smoothing for recorded GPS tracks. The raw points stay untouched in the
/// database; this pipeline produces a cleaned view of a track for distance calculation and
/// display. It removes GPS noise (jitter while standing, accuracy outliers, physically
/// impossible jumps) and applies a light moving-average so the polyline follows the real path.
/// </summary>
public static class TrackSmoother
{
    /// <summary>Points with a worse (higher) accuracy than this are dropped.</summary>
    public const double MaxAccuracyMeters = 25.0;

    /// <summary>Points closer than this to the previous kept point are dropped (standing jitter).</summary>
    public const double MinDistanceMeters = 4.0;

    /// <summary>Jumps faster than this between consecutive points are treated as outliers.</summary>
    public const double MaxSpeedMetersPerSecond = 30.0; // ~108 km/h, generous for sport tracks

    /// <summary>Half-window of the moving average (2 => averages up to 5 points).</summary>
    public const int SmoothingHalfWindow = 2;

    /// <summary>
    /// Returns a smoothed copy of <paramref name="track"/> (ordered by timestamp). The input
    /// list is not modified. Speed/altitude of kept points are carried over from the nearest
    /// original point; only the coordinates are smoothed.
    /// </summary>
    public static List<LocationModel> Smooth(IReadOnlyList<LocationModel> track)
    {
        if (track.Count < 3)
        {
            return track.ToList();
        }

        var ordered = track.OrderBy(l => l.Timestamp).ToList();

        List<LocationModel> filtered = FilterNoise(ordered);

        if (filtered.Count < 3)
        {
            return filtered;
        }

        return MovingAverage(filtered);
    }

    /// <summary>Distance in meters of the smoothed track.</summary>
    public static double SmoothedDistanceMeters(IReadOnlyList<LocationModel> track)
    {
        var smoothed = Smooth(track);
        double meters = 0;
        for (var i = 1; i < smoothed.Count; i++)
        {
            meters += HaversineMeters(smoothed[i - 1], smoothed[i]);
        }
        return meters;
    }

    /// <summary>
    /// Removes accuracy outliers, standing jitter and physically impossible jumps. Keeps the
    /// first point as anchor and walks forward, only accepting plausible successors.
    /// </summary>
    private static List<LocationModel> FilterNoise(List<LocationModel> ordered)
    {
        var kept = new List<LocationModel>();

        LocationModel? anchor = null;
        foreach (var point in ordered)
        {
            // Drop points with unknown or bad accuracy — they are the main source of wild zick-zack.
            if (point.Accuracy > 0 && point.Accuracy > MaxAccuracyMeters)
            {
                continue;
            }

            if (anchor is null)
            {
                kept.Add(point);
                anchor = point;
                continue;
            }

            double meters = HaversineMeters(anchor, point);
            double seconds = (point.Timestamp - anchor.Timestamp).TotalSeconds;

            // Standing jitter: ignore points that didn't move meaningfully.
            if (meters < MinDistanceMeters)
            {
                continue;
            }

            // Outlier: a jump that would require impossible speed (teleport GPS fixes).
            if (seconds > 0 && meters / seconds > MaxSpeedMetersPerSecond)
            {
                continue;
            }

            kept.Add(point);
            anchor = point;
        }

        // Never swallow the recorded end. When the user stops recording the final fixes
        // lie within the standing-jitter window of the last kept anchor, so they get
        // dropped and the rendered route stops short of where the trip actually ended.
        // Re-append the last recorded point (when it has usable accuracy) so the track
        // reaches its true endpoint.
        if (ordered.Count > 0
            && kept.Count > 0
            && !ReferenceEquals(kept[kept.Count - 1], ordered[ordered.Count - 1])
            && IsUsable(ordered[ordered.Count - 1])
            && !IsImplausibleJump(kept[kept.Count - 1], ordered[ordered.Count - 1]))
        {
            kept.Add(ordered[ordered.Count - 1]);
        }

        return kept;
    }

    private static bool IsUsable(LocationModel point)
        => point.Accuracy <= 0 || point.Accuracy <= MaxAccuracyMeters;

    /// <summary>
    /// True when the step from <paramref name="anchor"/> to <paramref name="point"/> would require
    /// a physically impossible speed. The re-append of the last recorded point must not resurrect a
    /// point that was rejected for exactly this reason — that re-introduced the teleport and added a
    /// spurious segment to the rendered route.
    /// </summary>
    private static bool IsImplausibleJump(LocationModel anchor, LocationModel point)
    {
        double seconds = (point.Timestamp - anchor.Timestamp).TotalSeconds;
        return seconds > 0 && HaversineMeters(anchor, point) / seconds > MaxSpeedMetersPerSecond;
    }

    /// <summary>
    /// Light moving average over the coordinates (lat/lon/altitude). Timestamps, speed and
    /// accuracy stay those of the center point so the track's timeline is preserved.
    /// </summary>
    private static List<LocationModel> MovingAverage(List<LocationModel> points)
    {
        var result = new List<LocationModel>(points.Count);

        for (var i = 0; i < points.Count; i++)
        {
            // Never pull the start/end of the track inward. The first and last points are the
            // recorded boundaries of the trip; averaging them with their neighbours drags the
            // rendered route metres away from where the user actually started/stopped (most
            // visible when a recording gap or a stationary cluster sits next to an endpoint).
            if (i == 0 || i == points.Count - 1)
            {
                var end = points[i];
                result.Add(new LocationModel
                {
                    ID = end.ID,
                    TripID = end.TripID,
                    Latitude = end.Latitude,
                    Longitude = end.Longitude,
                    Altitude = end.Altitude,
                    Accuracy = end.Accuracy,
                    Speed = end.Speed,
                    Heading = end.Heading,
                    HeadingAccuracy = end.HeadingAccuracy,
                    SpeedAccuracy = end.SpeedAccuracy,
                    Timestamp = end.Timestamp
                });
                continue;
            }

            int from = Math.Max(0, i - SmoothingHalfWindow);
            int to = Math.Min(points.Count - 1, i + SmoothingHalfWindow);

            double lat = 0, lon = 0, alt = 0;
            for (var j = from; j <= to; j++)
            {
                lat += points[j].Latitude;
                lon += points[j].Longitude;
                alt += points[j].Altitude;
            }
            int count = to - from + 1;

            var center = points[i];
            result.Add(new LocationModel
            {
                ID = center.ID,
                TripID = center.TripID,
                Latitude = lat / count,
                Longitude = lon / count,
                Altitude = alt / count,
                Accuracy = center.Accuracy,
                Speed = center.Speed,
                Heading = center.Heading,
                HeadingAccuracy = center.HeadingAccuracy,
                SpeedAccuracy = center.SpeedAccuracy,
                Timestamp = center.Timestamp
            });
        }

        return result;
    }

    private static double HaversineMeters(LocationModel a, LocationModel b)
    {
        const double earthRadiusMeters = 6371000;
        double dLat = (b.Latitude - a.Latitude) * Math.PI / 180;
        double dLon = (b.Longitude - a.Longitude) * Math.PI / 180;
        double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(a.Latitude * Math.PI / 180) * Math.Cos(b.Latitude * Math.PI / 180)
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }
}
