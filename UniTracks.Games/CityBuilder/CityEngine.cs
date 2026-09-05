using UniTracks.Games.Shared.Economy;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Games.CityBuilder;

/// <summary>
/// Pure game logic for the cozy city builder. No MAUI, SkiaSharp or EF dependencies —
/// takes persisted buildings plus activity stats and produces the city state,
/// and validates placement/demolition attempts.
/// </summary>
public static class CityEngine
{
    /// <summary>
    /// Rebuilds the full city state from persisted buildings, purchased expansions and activity stats.
    /// </summary>
    /// <param name="placed">Persisted buildings (from the repository).</param>
    /// <param name="expansions">Purchased grid expansions.</param>
    /// <param name="stats">Lifetime activity (trips with type info, XP, achievements).</param>
    public static CityState Rebuild(
        IEnumerable<PlacedBuilding> placed,
        IEnumerable<CityExpansion> expansions,
        ActivityStats stats)
    {
        var placedList = placed.ToList();
        var expansionList = expansions.ToList();
        int gridSize = CityExpansions.ResolveGridSize(expansionList.Select(e => e.GridSize));
        int earned = CoinEconomy.ComputeEarned(stats.Trips, stats.Xp, stats.UnlockedAchievements);
        int spent = ComputeSpent(placedList) + ComputeExpansionSpent(expansionList);

        var tiles = new List<CityTile>(gridSize * gridSize);
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var building = placedList.FirstOrDefault(p => p.X == x && p.Y == y);
                tiles.Add(new CityTile
                {
                    X = x,
                    Y = y,
                    PlacedBuildingId = building?.ID,
                    BuildingId = building?.BuildingId,
                    PlacedAt = building?.PlacedAt,
                });
            }
        }

        return new CityState
        {
            GridSize = gridSize,
            Tiles = tiles,
            CoinsEarned = earned,
            CoinsSpent = spent,
            Coins = Math.Max(0, earned - spent),
            Level = stats.Level,
            Xp = stats.Xp,
            UnlockedAchievementIds = stats.UnlockedAchievementIds,
        };
    }

    /// <summary>Coins currently invested in standing buildings (full cost minus demolition refunds).</summary>
    public static int ComputeSpent(IEnumerable<PlacedBuilding> placed) =>
        placed.Sum(p => BuildingCatalog.Find(p.BuildingId)?.Cost ?? 0);

    /// <summary>Coins invested in purchased expansions (priced via the progression table).</summary>
    public static int ComputeExpansionSpent(IEnumerable<CityExpansion> expansions) =>
        expansions.Sum(e => CityExpansions.Steps.FirstOrDefault(s => s.GridSize == e.GridSize)?.Cost ?? 0);

    /// <summary>Validates a placement request. Does not mutate anything.</summary>
    public static PlaceResult ValidatePlacement(CityState city, string buildingId, int x, int y)
    {
        var building = BuildingCatalog.Find(buildingId);
        if (building is null)
        {
            return PlaceResult.Fail(PlaceError.UnknownBuilding);
        }

        if (city.Level < building.RequiredLevel)
        {
            return PlaceResult.Fail(PlaceError.LevelTooLow);
        }

        if (building.RequiredAchievementId is not null && !city.UnlockedAchievementIds.Contains(building.RequiredAchievementId))
        {
            return PlaceResult.Fail(PlaceError.AchievementLocked);
        }

        var tile = city.GetTile(x, y);
        if (tile is null)
        {
            return PlaceResult.Fail(PlaceError.OutOfBounds);
        }

        if (!tile.IsEmpty)
        {
            return PlaceResult.Fail(PlaceError.TileOccupied);
        }

        if (city.Coins < building.Cost)
        {
            return PlaceResult.Fail(PlaceError.NotEnoughCoins);
        }

        return PlaceResult.Ok(city, building.Cost);
    }

    /// <summary>Validates a city-grid expansion purchase. Does not mutate anything.</summary>
    public static PlaceResult ValidateExpansion(CityState city)
    {
        var step = city.NextExpansion;
        if (step is null)
        {
            return PlaceResult.Fail(PlaceError.MaxSizeReached);
        }

        if (city.Level < step.RequiredLevel)
        {
            return PlaceResult.Fail(PlaceError.LevelTooLow);
        }

        if (city.Coins < step.Cost)
        {
            return PlaceResult.Fail(PlaceError.NotEnoughCoins);
        }

        return PlaceResult.Ok(city, step.Cost);
    }

    /// <summary>Validates a demolition request and computes the refund. Does not mutate anything.</summary>
    public static PlaceResult ValidateDemolition(CityState city, int x, int y)
    {
        var tile = city.GetTile(x, y);
        if (tile is null)
        {
            return PlaceResult.Fail(PlaceError.OutOfBounds);
        }

        if (tile.IsEmpty || tile.BuildingId is null)
        {
            return PlaceResult.Fail(PlaceError.TileEmpty);
        }

        var building = BuildingCatalog.Find(tile.BuildingId);
        int refund = CoinEconomy.DemolitionRefund(building?.Cost ?? 0);
        return PlaceResult.Ok(city, refund);
    }
}
