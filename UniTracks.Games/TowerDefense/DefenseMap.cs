namespace UniTracks.Games.TowerDefense;

/// <summary>What a tile is made of. Anything but <see cref="Grass"/> blocks tower placement.</summary>
public enum DefenseTileKind
{
    /// <summary>Buildable ground.</summary>
    Grass,

    /// <summary>The trail enemies march along.</summary>
    Path,

    /// <summary>Unbuildable water (lake / river).</summary>
    Water,

    /// <summary>Unbuildable forest / dense trees.</summary>
    Forest,
}

/// <summary>
/// A selectable trail-defense map: the fixed waypoint path enemies march along plus the
/// per-map difficulty modifiers. Positions are expressed in tile units with tile centers
/// at (x+0.5, y+0.5); paths are orthogonal (axis-aligned) so path membership and
/// distance resolution stay allocation-free and unambiguous.
/// </summary>
public sealed class DefenseMap
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string Icon { get; init; }

    /// <summary>1 (easy) … 5 (hard). Drives lives, enemy HP/speed scaling and pricing text.</summary>
    public int Difficulty { get; init; } = 1;

    /// <summary>Minimum gamification level required to play this map (1 = always available).</summary>
    public int RequiredLevel { get; init; } = 1;

    /// <summary>Achievement gates — any single one of these ids unlocks the map.</summary>
    public string[] RequiredAchievementIds { get; init; } = Array.Empty<string>();

    public required int GridWidth { get; init; }

    public required int GridHeight { get; init; }

    /// <summary>Waypoints in tile coordinates. The trail enters above the grid and leaves below it.</summary>
    public required (double X, double Y)[] Waypoints { get; init; }

    /// <summary>Lives the player starts a run on this map with.</summary>
    public int StartLives { get; init; } = 20;

    /// <summary>Multiplier applied on top of the per-wave HP scaling (higher = tougher).</summary>
    public double HpMultiplier { get; init; } = 1.0;

    /// <summary>Multiplier applied to enemy speed (higher = faster = tougher).</summary>
    public double SpeedMultiplier { get; init; } = 1.0;

    /// <summary>Water tiles (lakes, rivers) that cannot be built on, in tile coordinates.</summary>
    public (int X, int Y)[] Water { get; init; } = Array.Empty<(int, int)>();

    /// <summary>Forest tiles (dense trees) that cannot be built on, in tile coordinates.</summary>
    public (int X, int Y)[] Forest { get; init; } = Array.Empty<(int, int)>();

    /// <summary>Difficulty shown as stars in the map-selection UI (1 = ★, 5 = ★★★★★).</summary>
    public string Stars => new string('★', Difficulty);

    /// <summary>Number of grass tiles the player can actually build on (excludes trail/water/forest).</summary>
    public int BuildableCount
    {
        get
        {
            int count = 0;
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (TileKind(x, y) == DefenseTileKind.Grass)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

    private double[]? segmentLengths;
    private HashSet<(int, int)>? waterSet;
    private HashSet<(int, int)>? forestSet;

    /// <summary>Total path length in tile units. Enemies leak when their distance exceeds this.</summary>
    public double TotalLength => (segmentLengths ??= ComputeSegmentLengths()).Sum();

    /// <summary>Resolves a travelled distance to a position on the path (clamped at both ends).</summary>
    public (double X, double Y) PositionAt(double distance)
    {
        double remaining = Math.Clamp(distance, 0, TotalLength);
        for (int i = 0; i < segmentLengths!.Length; i++)
        {
            if (remaining <= segmentLengths[i])
            {
                double t = segmentLengths[i] <= 0 ? 0 : remaining / segmentLengths[i];
                return (
                    Waypoints[i].X + (Waypoints[i + 1].X - Waypoints[i].X) * t,
                    Waypoints[i].Y + (Waypoints[i + 1].Y - Waypoints[i].Y) * t);
            }

            remaining -= segmentLengths[i];
        }

        return Waypoints[^1];
    }

    /// <summary>True when the tile belongs to the trail (and therefore cannot be built on).</summary>
    public bool IsPath(int x, int y)
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

    /// <summary>The terrain of a tile — grass is buildable, everything else blocks placement.</summary>
    public DefenseTileKind TileKind(int x, int y)
    {
        if (IsPath(x, y))
        {
            return DefenseTileKind.Path;
        }

        if ((waterSet ??= new HashSet<(int, int)>(Water)).Contains((x, y)))
        {
            return DefenseTileKind.Water;
        }

        if ((forestSet ??= new HashSet<(int, int)>(Forest)).Contains((x, y)))
        {
            return DefenseTileKind.Forest;
        }

        return DefenseTileKind.Grass;
    }

    /// <summary>The waypoint the trail enters from (rendered as a start marker just off-grid).</summary>
    public (double X, double Y) Entry => Waypoints[0];

    /// <summary>The waypoint the trail exits at (rendered as a goal marker just off-grid).</summary>
    public (double X, double Y) Exit => Waypoints[^1];

    private double[] ComputeSegmentLengths()
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
