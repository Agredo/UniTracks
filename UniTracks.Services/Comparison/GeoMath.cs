namespace UniTracks.Services.Comparison;

/// <summary>
/// Geometry primitives shared by the trip-comparison pipeline: distances, bounds, centroids,
/// distance-based resampling and the shape distance used to tell two routes apart.
/// <para>
/// Deliberately dependency-free and allocation-frugal: it runs over every stored trip once
/// (fingerprint backfill) and then over the candidate pairs of a single comparison.
/// </para>
/// </summary>
public static class GeoMath
{
    public const double EarthRadiusMeters = 6371000.0;

    private const double DegreesToRadians = Math.PI / 180.0;

    /// <summary>Great-circle distance in meters.</summary>
    public static double HaversineMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double dLat = (latitude2 - latitude1) * DegreesToRadians;
        double dLon = (longitude2 - longitude1) * DegreesToRadians;

        double sinLat = Math.Sin(dLat / 2);
        double sinLon = Math.Sin(dLon / 2);

        double h = sinLat * sinLat
            + Math.Cos(latitude1 * DegreesToRadians) * Math.Cos(latitude2 * DegreesToRadians) * sinLon * sinLon;

        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    public static double HaversineMeters(GeoPoint a, GeoPoint b) =>
        HaversineMeters(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    /// <summary>Bounding box of a non-empty point list.</summary>
    public static GeoBounds Bounds(IReadOnlyList<GeoPoint> points)
    {
        if (points.Count == 0)
        {
            throw new ArgumentException("A bounding box needs at least one point.", nameof(points));
        }

        double minLat = points[0].Latitude, maxLat = minLat;
        double minLon = points[0].Longitude, maxLon = minLon;

        for (int i = 1; i < points.Count; i++)
        {
            var p = points[i];
            if (p.Latitude < minLat) minLat = p.Latitude;
            if (p.Latitude > maxLat) maxLat = p.Latitude;
            if (p.Longitude < minLon) minLon = p.Longitude;
            if (p.Longitude > maxLon) maxLon = p.Longitude;
        }

        return new GeoBounds(minLat, minLon, maxLat, maxLon);
    }

    /// <summary>
    /// Mean position of the points. Longitudes are averaged in the local metric frame (so a track
    /// crossing the 180th meridian does not average to the other side of the planet) before being
    /// converted back.
    /// </summary>
    public static GeoPoint Centroid(IReadOnlyList<GeoPoint> points)
    {
        if (points.Count == 0)
        {
            throw new ArgumentException("A centroid needs at least one point.", nameof(points));
        }

        double referenceLatitude = points[0].Latitude;
        double cosReference = Math.Cos(referenceLatitude * DegreesToRadians);

        double sumX = 0;
        double sumY = 0;
        foreach (var point in points)
        {
            sumY += point.Latitude;
            sumX += point.Longitude * cosReference;
        }

        double meanLatitude = sumY / points.Count;
        double meanX = sumX / points.Count;

        // A cosine of zero only occurs at the poles, where the metric frame collapses anyway.
        double longitude = cosReference == 0 ? meanX : meanX / cosReference;

        return new GeoPoint(meanLatitude, longitude);
    }

    /// <summary>
    /// Resamples a path to exactly <paramref name="targetCount"/> points spaced evenly along its
    /// <b>cumulative distance</b>. Two recordings of the same route therefore line up point by point
    /// even when they were sampled at different rates or contain different numbers of fixes.
    /// </summary>
    public static List<GeoPoint> ResampleByDistance(IReadOnlyList<GeoPoint> path, int targetCount)
    {
        if (targetCount < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(targetCount), "Resampling needs at least two target points.");
        }

        if (path.Count == 0)
        {
            return new List<GeoPoint>();
        }

        if (path.Count == 1)
        {
            return Enumerable.Repeat(path[0], targetCount).ToList();
        }

        // Cumulative distance along the path, so a target distance can be located in O(1).
        var cumulative = new double[path.Count];
        for (int i = 1; i < path.Count; i++)
        {
            cumulative[i] = cumulative[i - 1] + HaversineMeters(path[i - 1], path[i]);
        }

        double total = cumulative[path.Count - 1];

        // A stationary track (or one with duplicate points) has no length to divide up.
        if (total <= 0)
        {
            return Enumerable.Repeat(path[0], targetCount).ToList();
        }

        var resampled = new List<GeoPoint>(targetCount);
        int segment = 1;

        for (int i = 0; i < targetCount; i++)
        {
            double target = total * i / (targetCount - 1);

            while (segment < path.Count - 1 && cumulative[segment] < target)
            {
                segment++;
            }

            double segmentStart = cumulative[segment - 1];
            double segmentLength = cumulative[segment] - segmentStart;

            if (segmentLength <= 0)
            {
                resampled.Add(path[segment]);
                continue;
            }

            double t = (target - segmentStart) / segmentLength;
            var a = path[segment - 1];
            var b = path[segment];

            resampled.Add(new GeoPoint(
                a.Latitude + (b.Latitude - a.Latitude) * t,
                a.Longitude + (b.Longitude - a.Longitude) * t));
        }

        return resampled;
    }

    /// <summary>
    /// Discrete Fréchet distance in meters between two polylines of equal or different length.
    /// <para>
    /// Unlike a Hausdorff distance this is <b>order-aware</b>: an out-and-back and a loop over the
    /// same ground, or the same route run in the opposite direction, score far apart instead of
    /// looking identical. It also tolerates a detour, because it only ever measures how far the
    /// paths diverge where they have to, which is what "same route, slightly different line" means.
    /// </para>
    /// </summary>
    public static double FrechetMeters(IReadOnlyList<GeoPoint> pathA, IReadOnlyList<GeoPoint> pathB)
    {
        if (pathA.Count == 0 || pathB.Count == 0)
        {
            throw new ArgumentException("Fréchet distance needs two non-empty paths.");
        }

        if (pathA.Count == 1 && pathB.Count == 1)
        {
            return HaversineMeters(pathA[0], pathB[0]);
        }

        // Equirectangular projection into a local metric frame: the two paths are compared over a
        // few kilometres, where the flattening error is far below the GPS noise we are fighting.
        double referenceLatitude = (pathA[0].Latitude + pathB[0].Latitude) / 2;
        double metersPerRadian = EarthRadiusMeters * DegreesToRadians;
        double cosReference = Math.Cos(referenceLatitude * DegreesToRadians);

        var a = ProjectToMeters(pathA, metersPerRadian, cosReference);
        var b = ProjectToMeters(pathB, metersPerRadian, cosReference);

        // Discrete Fréchet via dynamic programming. Two rolling rows are enough, so the cost is
        // O(n*m) time and O(min(n,m)) memory — with the 32-point fingerprints that is trivial.
        var previous = new double[b.Length];
        var current = new double[b.Length];

        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < b.Length; j++)
            {
                double distance = Distance(a[i], b[j]);

                if (i == 0 && j == 0)
                {
                    current[0] = distance;
                    continue;
                }

                double best = double.MaxValue;
                if (i > 0 && j > 0) best = Math.Min(best, previous[j - 1]);
                if (i > 0) best = Math.Min(best, previous[j]);
                if (j > 0) best = Math.Min(best, current[j - 1]);

                current[j] = Math.Max(distance, best);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length - 1];
    }

    private static (double X, double Y)[] ProjectToMeters(IReadOnlyList<GeoPoint> path, double metersPerRadian, double cosReference)
    {
        var projected = new (double X, double Y)[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            projected[i] = (path[i].Longitude * cosReference * metersPerRadian, path[i].Latitude * metersPerRadian);
        }

        return projected;
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
