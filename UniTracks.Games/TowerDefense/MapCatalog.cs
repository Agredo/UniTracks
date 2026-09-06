namespace UniTracks.Games.TowerDefense;

/// <summary>
/// The five selectable trail-defense maps, ordered easiest to hardest. Each map defines its
/// own waypoint path, starting lives and enemy HP/speed multipliers. Lower-difficulty maps
/// offer an open, mostly straight trail with plenty of build space; harder maps wind around
/// and shorten the reaction window.
/// </summary>
public static class MapCatalog
{
    public static readonly DefenseMap[] All =
    {
        // Waldwiese — fast komplett offen, nur eine kleine Lichtung mit Bäumen in der
        // unteren rechten Ecke. Maximaler Bauplatz, ideale Einstiegskarte.
        new()
        {
            Id = "waldwiese",
            Name = "Waldwiese",
            Description = "Ein offener, gerader Pfad mit viel Platz für Türme.",
            Icon = "🌲",
            Difficulty = 1,
            GridWidth = 9,
            GridHeight = 15,
            Waypoints = new[]
            {
                (2.5, -0.5), (2.5, 5.5), (6.5, 5.5), (6.5, 10.5), (3.5, 10.5), (3.5, 15.5),
            },
            Forest = new[]
            {
                (7, 11), (8, 11),
                (7, 12), (8, 12),
            },
            StartLives = 25,
            HpMultiplier = 1.0,
            SpeedMultiplier = 1.0,
        },

        // Park-Promenade — ein kleiner Weiher in der linken unteren Ecke schmälert den
        // Bauplatz leicht; ein paar Bäume säumen die rechte Seite.
        new()
        {
            Id = "park-promenade",
            Name = "Park-Promenade",
            Description = "Ein gemütlicher S-förmiger Weg durch den Park mit einem Weiher.",
            Icon = "🌸",
            Difficulty = 2,
            RequiredLevel = 2,
            GridWidth = 9,
            GridHeight = 15,
            Waypoints = new[]
            {
                (2.5, -0.5), (2.5, 4.5), (6.5, 4.5), (6.5, 8.5), (2.5, 8.5), (2.5, 12.5), (6.5, 12.5), (6.5, 15.5),
            },
            Water = new[]
            {
                (0, 9), (1, 9),
                (0, 10), (1, 10),
                (0, 11), (1, 11),
            },
            Forest = new[]
            {
                (7, 1), (7, 2),
            },
            StartLives = 20,
            HpMultiplier = 1.1,
            SpeedMultiplier = 1.05,
        },

        // Seeufer — ein großer See in der linken unteren Ecke und Wald oben rechts lassen
        // nur noch einen schmalen Bau-Streifen frei.
        new()
        {
            Id = "seeufer",
            Name = "Seeufer",
            Description = "Ein gewundener Uferweg am großen See — wenig Platz am Ufer.",
            Icon = "🌊",
            Difficulty = 3,
            RequiredAchievementIds = new[] { "streak-3" },
            GridWidth = 9,
            GridHeight = 15,
            Waypoints = new[]
            {
                (2.5, -0.5), (2.5, 3.5), (6.5, 3.5), (6.5, 6.5), (3.5, 6.5), (3.5, 11.5), (6.5, 11.5), (6.5, 15.5),
            },
            Water = new[]
            {
                (0, 7), (1, 7), (2, 7),
                (0, 8), (1, 8), (2, 8),
                (0, 9), (1, 9), (2, 9),
                (0, 10), (1, 10), (2, 10),
                (0, 11), (1, 11),
                (0, 12), (1, 12),
            },
            Forest = new[]
            {
                (7, 0), (8, 0),
                (8, 1),
                (7, 7), (8, 7),
                (7, 8), (8, 8),
            },
            StartLives = 18,
            HpMultiplier = 1.25,
            SpeedMultiplier = 1.12,
        },

        // Altstadt-Gasse — Bebauung (= Wald) blockiert die freien Flächen zwischen den
        // Gassen, dazu ein Kanal unten. Kaum noch Bauplatz.
        new()
        {
            Id = "altstadt-gasse",
            Name = "Altstadt-Gasse",
            Description = "Enge Gassen zwischen Häuserblocks — kaum Platz zum Bauen.",
            Icon = "🏰",
            Difficulty = 4,
            RequiredAchievementIds = new[] { "fifty-km" },
            GridWidth = 9,
            GridHeight = 15,
            Waypoints = new[]
            {
                (2.5, -0.5), (2.5, 2.5), (6.5, 2.5), (6.5, 5.5), (2.5, 5.5), (2.5, 8.5), (6.5, 8.5), (6.5, 11.5), (2.5, 11.5), (2.5, 15.5),
            },
            Forest = new[]
            {
                (0, 0), (1, 0),
                (0, 1), (1, 1),
                (7, 6), (8, 6),
                (7, 7), (8, 7),
                (0, 9), (1, 9),
                (0, 10), (1, 10),
                (3, 13), (4, 13), (5, 13), (6, 13), (7, 13),
                (3, 14), (4, 14), (5, 14), (6, 14), (7, 14),
            },
            Water = new[]
            {
                (8, 13),
                (8, 14),
            },
            StartLives = 16,
            HpMultiplier = 1.4,
            SpeedMultiplier = 1.18,
        },

        // Streifzug — ein reißender Fluss (Wasser) in der unteren Hälfte plus dichter Wald
        // lassen nur winzige Bau-Inseln am Weg. Der härteste Trail.
        new()
        {
            Id = "streifzug",
            Name = "Streifzug",
            Description = "Der härteste Trail: Fluss und dichter Wald — nur winzige Bau-Inseln.",
            Icon = "⛰️",
            Difficulty = 5,
            RequiredAchievementIds = new[] { "hundred-km", "streak-7" },
            GridWidth = 9,
            GridHeight = 15,
            Waypoints = new[]
            {
                (2.5, -0.5), (2.5, 3.5), (6.5, 3.5), (6.5, 6.5), (3.5, 6.5), (3.5, 9.5), (6.5, 9.5), (6.5, 13.5), (2.5, 13.5), (2.5, 15.5),
            },
            Water = new[]
            {
                (0, 10), (1, 10),
                (0, 11), (1, 11),
                (0, 12), (1, 12),
                (0, 13), (1, 13),
                (0, 14), (1, 14),
            },
            Forest = new[]
            {
                (7, 0), (8, 0),
                (7, 1), (8, 1),
                (8, 2),
                (0, 4), (1, 4),
                (1, 5),
                (0, 6), (1, 6),
                (5, 5),
                (4, 7), (5, 7),
                (4, 8), (5, 8),
                (0, 9),
                (7, 14), (8, 14),
            },
            StartLives = 14,
            HpMultiplier = 1.55,
            SpeedMultiplier = 1.25,
        },
    };

    /// <summary>The map used when none is selected yet (the easiest one).</summary>
    public static DefenseMap Default => All[0];

    /// <summary>Finds a map by id, falling back to <see cref="Default"/> for unknown ids.</summary>
    public static DefenseMap Find(string? id)
    {
        foreach (var map in All)
        {
            if (string.Equals(map.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return map;
            }
        }

        return Default;
    }
}
