namespace UniTracks.Services.Comparison;

/// <summary>
/// Geohash encoding used as the spatial index for route matching.
/// <para>
/// A trip stores the set of cells its track touches. Two trips that cover the same ground share
/// most of their cells, which makes "which other trips went through here?" a cheap set comparison
/// instead of an all-pairs geometry test.
/// </para>
/// </summary>
public static class Geohash
{
    /// <summary>Geohash base32 alphabet (the digits and lower-case letters without a, i, l and o).</summary>
    private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

    /// <summary>Precision whose cells are roughly 150 m across — the scale of a GPS-drift-tolerant route cell.</summary>
    public const int RoutePrecision = 7;

    /// <summary>Precision whose cells are roughly 5 km across — enough to recognise the neighbourhood a trip starts in.</summary>
    public const int AreaPrecision = 5;

    public const int MinPrecision = 1;
    public const int MaxPrecision = 12;

    public static string Encode(double latitude, double longitude, int precision)
    {
        if (precision is < MinPrecision or > MaxPrecision)
        {
            throw new ArgumentOutOfRangeException(nameof(precision), precision, $"Geohash precision must be between {MinPrecision} and {MaxPrecision}.");
        }

        // The poles and the antimeridian are not walkable terrain; clamping keeps the bisection honest.
        latitude = Math.Clamp(latitude, -90, 90);
        longitude = Math.Clamp(longitude, -180, 180);

        double minLatitude = -90, maxLatitude = 90;
        double minLongitude = -180, maxLongitude = 180;

        var hash = new char[precision];
        int character = 0;
        int bit = 0;
        int value = 0;
        bool evenBit = true;

        while (character < precision)
        {
            if (evenBit)
            {
                double middle = (minLongitude + maxLongitude) / 2;
                if (longitude >= middle)
                {
                    value = (value << 1) | 1;
                    minLongitude = middle;
                }
                else
                {
                    value <<= 1;
                    maxLongitude = middle;
                }
            }
            else
            {
                double middle = (minLatitude + maxLatitude) / 2;
                if (latitude >= middle)
                {
                    value = (value << 1) | 1;
                    minLatitude = middle;
                }
                else
                {
                    value <<= 1;
                    maxLatitude = middle;
                }
            }

            evenBit = !evenBit;

            if (++bit == 5)
            {
                hash[character++] = Base32[value];
                bit = 0;
                value = 0;
            }
        }

        return new string(hash);
    }

    public static string Encode(GeoPoint point, int precision) => Encode(point.Latitude, point.Longitude, precision);

    /// <summary>Cell bounds of a geohash. Returns false for a hash that is not valid base32.</summary>
    public static bool TryGetBounds(string hash, out GeoBounds bounds)
    {
        bounds = default;

        if (string.IsNullOrEmpty(hash) || hash.Length > MaxPrecision)
        {
            return false;
        }

        double minLatitude = -90, maxLatitude = 90;
        double minLongitude = -180, maxLongitude = 180;
        bool evenBit = true;

        foreach (char c in hash)
        {
            int value = Base32.IndexOf(char.ToLowerInvariant(c));
            if (value < 0)
            {
                return false;
            }

            for (int mask = 16; mask > 0; mask >>= 1)
            {
                bool high = (value & mask) != 0;

                if (evenBit)
                {
                    double middle = (minLongitude + maxLongitude) / 2;
                    if (high) minLongitude = middle; else maxLongitude = middle;
                }
                else
                {
                    double middle = (minLatitude + maxLatitude) / 2;
                    if (high) minLatitude = middle; else maxLatitude = middle;
                }

                evenBit = !evenBit;
            }
        }

        bounds = new GeoBounds(minLatitude, minLongitude, maxLatitude, maxLongitude);
        return true;
    }

    public static GeoPoint DecodeCenter(string hash)
    {
        if (!TryGetBounds(hash, out var bounds))
        {
            throw new ArgumentException($"'{hash}' is not a valid geohash.", nameof(hash));
        }

        return new GeoPoint(bounds.CenterLatitude, bounds.CenterLongitude);
    }

    /// <summary>
    /// The cell itself plus its eight neighbours. Two recordings of the same route rarely start in
    /// exactly the same cell (one may begin 40 m before the other), so candidate lookup has to look
    /// one cell sideways or it silently drops the runs we care about.
    /// </summary>
    public static IReadOnlyList<string> Neighborhood(string hash)
    {
        if (!TryGetBounds(hash, out var bounds))
        {
            return Array.Empty<string>();
        }

        double height = bounds.MaxLatitude - bounds.MinLatitude;
        double width = bounds.MaxLongitude - bounds.MinLongitude;
        double centerLatitude = bounds.CenterLatitude;
        double centerLongitude = bounds.CenterLongitude;

        var cells = new List<string>(9) { hash };

        for (int latitudeStep = -1; latitudeStep <= 1; latitudeStep++)
        {
            for (int longitudeStep = -1; longitudeStep <= 1; longitudeStep++)
            {
                if (latitudeStep == 0 && longitudeStep == 0)
                {
                    continue;
                }

                double latitude = centerLatitude + latitudeStep * height;
                double longitude = centerLongitude + longitudeStep * width;

                // Stepping past a pole leaves the domain; skip rather than encode a wrapped cell.
                if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
                {
                    continue;
                }

                cells.Add(Encode(latitude, longitude, hash.Length));
            }
        }

        return cells;
    }

    /// <summary>
    /// Deduplicated route cells for a track. Consecutive points inside the same cell collapse, so
    /// the set size reflects the ground covered rather than the GPS sampling rate.
    /// </summary>
    public static string[] RouteCells(IReadOnlyList<GeoPoint> path, int precision = RoutePrecision)
    {
        if (path.Count == 0)
        {
            return Array.Empty<string>();
        }

        var cells = new List<string>();
        string? previous = null;

        foreach (var point in path)
        {
            string cell = Encode(point, precision);
            if (cell != previous)
            {
                if (!cells.Contains(cell))
                {
                    cells.Add(cell);
                }

                previous = cell;
            }
        }

        return cells.ToArray();
    }
}
