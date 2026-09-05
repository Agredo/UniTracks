using UniTracks.Games.Economy;
using UniTracks.Games.Persistence;

namespace UniTracks.Games.CityBuilder;

/// <summary>
/// Pure game logic for the cozy city builder. No MAUI, SkiaSharp or EF dependencies —
/// takes persisted buildings plus activity stats and produces the city state,
/// and validates placement/demolition attempts.
/// </summary>
public static class CityEngine
{
    /// <summary>
    /// Rebuilds the full city state from persisted buildings and activity stats.
    /// </summary>
    /// <param name="placed">Persisted buildings (from the repository).</param>
    /// <param name="totalDistanceKm">Lifetime distance across all trips.</param>
    /// <param name="totalTrips">Lifetime trip count.</param>
    /// <param name="unlockedAchievements">Number of unlocked achievements.</param>
    /// <param name="gridSize">Edge length of the square city grid.</param>
    public static CityState Rebuild(
        IEnumerable<PlacedBuilding> placed,
        double totalDistanceKm,
        int totalTrips,
        int unlockedAchievements,
        int gridSize = CityState.DefaultGridSize)
    {
        int earned = CoinEconomy.ComputeEarned(totalDistanceKm, totalTrips, unlockedAchievements);
        int spent = ComputeSpent(placed);

        var tiles = new List<CityTile>(gridSize * gridSize);
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var building = placed.FirstOrDefault(p => p.X == x && p.Y == y);
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
        };
    }

    /// <summary>Coins currently invested in standing buildings (full cost minus demolition refunds).</summary>
    public static int ComputeSpent(IEnumerable<PlacedBuilding> placed) =>
        placed.Sum(p => BuildingCatalog.Find(p.BuildingId)?.Cost ?? 0);

    /// <summary>Validates a placement request. Does not mutate anything.</summary>
    public static PlaceResult ValidatePlacement(CityState city, string buildingId, int x, int y)
    {
        var building = BuildingCatalog.Find(buildingId);
        if (building is null)
        {
            return PlaceResult.Fail(PlaceError.UnknownBuilding);
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
