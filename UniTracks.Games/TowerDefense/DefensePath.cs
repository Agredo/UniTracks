namespace UniTracks.Games.TowerDefense;

/// <summary>
/// The fixed trail the enemies march along. The path is defined by waypoints on a
/// top-down grid; positions are expressed in tile units with tile centers at (x+0.5, y+0.5).
/// Pure geometry — computing a position from a travelled distance never allocates.
/// </summary>
public static class DefensePath
{
    public const int GridWidth = 9;

    public const int GridHeight = 15;

    /// <summary>Waypoints in tile coordinates. The trail enters above the grid and leaves below it.</summary>
    private static readonly (double X, double Y)[] Waypoints =
    {
        (2.5, -0.5),
        (2.5, 4.5),
        (6.5, 4.5),
        (6.5, 8.5),
        (2.5, 8.5),
        (2.5, 12.5),
        (6.5, 12.5),
        (6.5, 15.5),
    };

    private static readonly double[] SegmentLengths = ComputeSegmentLengths();

    /// <summary>Total path length in tile units. Enemies leak when their distance exceeds this.</summary>
    public static double TotalLength { get; } = SegmentLengths.Sum();

    /// <summary>Resolves a travelled distance to a position on the path (clamped at both ends).</summary>
    public static (double X, double Y) PositionAt(double distance)
    {
        double remaining = Math.Clamp(distance, 0, TotalLength);
        for (int i = 0; i < SegmentLengths.Length; i++)
        {
            if (remaining <= SegmentLengths[i])
            {
                double t = SegmentLengths[i] <= 0 ? 0 : remaining / SegmentLengths[i];
                return (
                    Waypoints[i].X + (Waypoints[i + 1].X - Waypoints[i].X) * t,
                    Waypoints[i].Y + (Waypoints[i + 1].Y - Waypoints[i].Y) * t);
            }

            remaining -= SegmentLengths[i];
        }

        return Waypoints[^1];
    }

    /// <summary>True when the tile belongs to the trail (and therefore cannot be built on).</summary>
    public static bool IsPath(int x, int y)
    {
        for (int i = 0; i < Waypoints.Length - 1; i++)
        {
            var (x1, y1) = Waypoints[i];
            var (x2, y2) = Waypoints[i + 1];
            int minX = (int)Math.Floor(Math.Min(x1, x2));
            int maxX = (int)Math.Floor(Math.Max(x1, x2));
            int minY = (int)Math.Floor(Math.Min(y1, y2));
            int maxY = (int)Math.Floor(Math.Max(y1, y2));
            if (x >= minX && x <= maxX && y >= minY && y <= maxY)
            {
                return true;
            }
        }

        return false;
    }

    private static double[] ComputeSegmentLengths()
    {
        var lengths = new double[Waypoints.Length - 1];
        for (int i = 0; i < lengths.Length; i++)
        {
            lengths[i] = Math.Abs(Waypoints[i + 1].X - Waypoints[i].X)
                       + Math.Abs(Waypoints[i + 1].Y - Waypoints[i].Y);
        }

        return lengths;
    }
}
