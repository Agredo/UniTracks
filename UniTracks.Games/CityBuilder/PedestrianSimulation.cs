namespace UniTracks.Games.CityBuilder;

/// <summary>Kind of pedestrian — children are smaller, faster and stay near their play/school anchor.</summary>
public enum PedestrianKind
{
    Adult,
    Child,
}

/// <summary>
/// A single ambient pedestrian wandering the city in tile space.
/// Position for rendering is the interpolated <see cref="RenderX"/>/<see cref="RenderY"/>.
/// </summary>
public sealed class Pedestrian
{
    /// <summary>Stable key — identifies the pedestrian across city rebuilds (anchor building + index).</summary>
    public required string Key { get; init; }

    public PedestrianKind Kind { get; init; }

    /// <summary>Tile the pedestrian stays close to (home / playground / school).</summary>
    public int AnchorX { get; init; }
    public int AnchorY { get; init; }

    /// <summary>Palette index — the renderer picks clothing colors from it.</summary>
    public int ColorIndex { get; init; }

    /// <summary>Phase offset so figures don't bob in sync.</summary>
    public double Phase { get; init; }

    public int FromX { get; set; }
    public int FromY { get; set; }
    public int ToX { get; set; }
    public int ToY { get; set; }

    /// <summary>0..1 progress of the current tile-to-tile walk.</summary>
    public double Progress { get; set; } = 1;

    /// <summary>Remaining idle pause in seconds before the next walk starts.</summary>
    public double IdleSeconds { get; set; }

    public double RenderX => FromX + (ToX - FromX) * Progress;
    public double RenderY => FromY + (ToY - FromY) * Progress;

    public bool IsWalking => Progress < 1;
}

/// <summary>
/// Ambient pedestrian simulation for the cozy city map, in pure tile space (no rendering concerns).
/// Population is derived from the placed buildings: houses/villas spawn residents, shops and cafés
/// attract visitors, playgrounds and schools spawn children that stay near their anchor.
/// </summary>
public sealed class PedestrianSimulation
{
    /// <summary>Hard cap so the map never feels crowded and redraw cost stays bounded.</summary>
    public const int MaxPedestrians = 15;

    /// <summary>How far children may roam from their anchor (Chebyshev distance in tiles).</summary>
    private const int ChildRoamRadius = 2;

    /// <summary>How far adults may roam from home.</summary>
    private const int AdultRoamRadius = 4;

    private const double AdultTilesPerSecond = 0.9;
    private const double ChildTilesPerSecond = 1.4;

    private readonly Random random = new();
    private readonly Dictionary<string, Pedestrian> pedestrians = new();

    /// <summary>Current pedestrians — updated by <see cref="Update"/>.</summary>
    public IReadOnlyCollection<Pedestrian> Pedestrians => pedestrians.Values;

    /// <summary>Syncs the population with the city and advances all pedestrians by <paramref name="dtSeconds"/>.</summary>
    public void Update(CityState city, double dtSeconds)
    {
        SyncPopulation(city);

        foreach (var pedestrian in pedestrians.Values)
        {
            Advance(pedestrian, city.GridSize, dtSeconds);
        }
    }

    private void SyncPopulation(CityState city)
    {
        var wanted = new List<Pedestrian>();

        foreach (var tile in city.Tiles.Where(t => !t.IsEmpty))
        {
            switch (tile.BuildingId)
            {
                case "house":
                    wanted.Add(Create(tile, 0, PedestrianKind.Adult));
                    break;
                case "villa":
                    wanted.Add(Create(tile, 0, PedestrianKind.Adult));
                    wanted.Add(Create(tile, 1, PedestrianKind.Adult));
                    break;
                case "cafe":
                case "shop":
                    wanted.Add(Create(tile, 0, PedestrianKind.Adult));
                    break;
                case "playground":
                    wanted.Add(Create(tile, 0, PedestrianKind.Child));
                    wanted.Add(Create(tile, 1, PedestrianKind.Child));
                    wanted.Add(Create(tile, 2, PedestrianKind.Child));
                    break;
                case "school":
                    wanted.Add(Create(tile, 0, PedestrianKind.Child));
                    wanted.Add(Create(tile, 1, PedestrianKind.Child));
                    break;
            }

            if (wanted.Count >= MaxPedestrians)
            {
                break;
            }
        }

        // Drop pedestrians whose anchor building disappeared.
        var wantedKeys = wanted.Select(p => p.Key).ToHashSet();
        foreach (var stale in pedestrians.Keys.Where(k => !wantedKeys.Contains(k)).ToList())
        {
            pedestrians.Remove(stale);
        }

        // Keep existing pedestrians (preserves their position), spawn newcomers on their anchor tile.
        foreach (var want in wanted.Take(MaxPedestrians))
        {
            if (!pedestrians.ContainsKey(want.Key))
            {
                pedestrians[want.Key] = want;
            }
        }
    }

    private Pedestrian Create(CityTile anchor, int index, PedestrianKind kind) =>
        new()
        {
            Key = $"{anchor.PlacedBuildingId}:{index}",
            Kind = kind,
            AnchorX = anchor.X,
            AnchorY = anchor.Y,
            ColorIndex = Math.Abs((anchor.X * 7 + anchor.Y * 13 + index * 5) % 6),
            Phase = random.NextDouble() * Math.PI * 2,
            FromX = anchor.X,
            FromY = anchor.Y,
            ToX = anchor.X,
            ToY = anchor.Y,
            IdleSeconds = random.NextDouble() * 2,
        };

    private void Advance(Pedestrian pedestrian, int gridSize, double dtSeconds)
    {
        if (pedestrian.IsWalking)
        {
            double speed = pedestrian.Kind == PedestrianKind.Child ? ChildTilesPerSecond : AdultTilesPerSecond;
            pedestrian.Progress += speed * dtSeconds;
            if (pedestrian.Progress >= 1)
            {
                pedestrian.Progress = 1;
                pedestrian.FromX = pedestrian.ToX;
                pedestrian.FromY = pedestrian.ToY;
                pedestrian.IdleSeconds = pedestrian.Kind == PedestrianKind.Child
                    ? random.NextDouble() * 1.5
                    : 1 + random.NextDouble() * 3;
            }

            return;
        }

        pedestrian.IdleSeconds -= dtSeconds;
        if (pedestrian.IdleSeconds <= 0)
        {
            PickNextTarget(pedestrian, gridSize);
        }
    }

    private void PickNextTarget(Pedestrian pedestrian, int gridSize)
    {
        int radius = pedestrian.Kind == PedestrianKind.Child ? ChildRoamRadius : AdultRoamRadius;

        var candidates = new[]
            {
                (pedestrian.FromX + 1, pedestrian.FromY),
                (pedestrian.FromX - 1, pedestrian.FromY),
                (pedestrian.FromX, pedestrian.FromY + 1),
                (pedestrian.FromX, pedestrian.FromY - 1),
            }
            .Where(c => c.Item1 >= 0 && c.Item2 >= 0 && c.Item1 < gridSize && c.Item2 < gridSize)
            .Where(c => Math.Max(Math.Abs(c.Item1 - pedestrian.AnchorX), Math.Abs(c.Item2 - pedestrian.AnchorY)) <= radius)
            .ToArray();

        if (candidates.Length == 0)
        {
            pedestrian.IdleSeconds = 1;
            return;
        }

        var (tx, ty) = candidates[random.Next(candidates.Length)];
        pedestrian.ToX = tx;
        pedestrian.ToY = ty;
        pedestrian.Progress = 0;
    }
}
