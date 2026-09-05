namespace UniTracks.Games.CityBuilder;

/// <summary>One purchasable city-grid expansion step (level gate + coin price).</summary>
public record CityExpansionStep
{
    /// <summary>Grid edge length after purchasing this step.</summary>
    public int GridSize { get; init; }

    /// <summary>Gamification level required to unlock the purchase option.</summary>
    public int RequiredLevel { get; init; }

    /// <summary>Coin price of the expansion.</summary>
    public int Cost { get; init; }
}

/// <summary>Static progression table for city-grid expansions.</summary>
public static class CityExpansions
{
    public static IReadOnlyList<CityExpansionStep> Steps { get; } = new List<CityExpansionStep>
    {
        new() { GridSize = 8, RequiredLevel = 2, Cost = 300 },
        new() { GridSize = 10, RequiredLevel = 4, Cost = 800 },
        new() { GridSize = 12, RequiredLevel = 7, Cost = 1500 },
    };

    /// <summary>The next purchasable step beyond the current grid size, or null when maxed out.</summary>
    public static CityExpansionStep? NextStep(int currentGridSize) =>
        Steps.Where(s => s.GridSize > currentGridSize).OrderBy(s => s.GridSize).FirstOrDefault();

    /// <summary>Largest grid size reachable with the given purchased sizes (falls back to default).</summary>
    public static int ResolveGridSize(IEnumerable<int> purchasedSizes)
    {
        int max = CityState.DefaultGridSize;
        foreach (int size in purchasedSizes)
        {
            // Only contiguous progression counts: 8 unlocks before 10, etc.
            var expected = NextStep(max);
            if (expected is null || size != expected.GridSize)
            {
                continue;
            }

            max = size;
        }

        return max;
    }
}
