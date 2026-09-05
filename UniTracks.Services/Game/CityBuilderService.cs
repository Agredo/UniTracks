using UniTracks.Games.CityBuilder;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.Shared.Persistence;

namespace UniTracks.Services.Game;

/// <summary>
/// Coordinates the pure <see cref="CityEngine"/> logic with persistence: every mutation
/// is validated first, persisted, and answered with a freshly rebuilt city state so the
/// coin balance (earned − spent) always stays consistent.
/// </summary>
public class CityBuilderService : ICityBuilderService
{
    private readonly ICityStore cityStore;
    private readonly IActivityStatsSource activityStats;

    public CityBuilderService(ICityStore cityStore, IActivityStatsSource activityStats)
    {
        this.cityStore = cityStore;
        this.activityStats = activityStats;
    }

    public async Task<CityState> GetCityAsync()
    {
        var placed = await cityStore.LoadAsync();
        var expansions = await cityStore.LoadExpansionsAsync();
        var stats = await activityStats.GetAsync();
        return CityEngine.Rebuild(placed, expansions, stats);
    }

    public async Task<PlaceResult> TryPlaceAsync(string buildingId, int x, int y)
    {
        var city = await GetCityAsync();
        var validation = CityEngine.ValidatePlacement(city, buildingId, x, y);
        if (!validation.Success)
        {
            return validation;
        }

        await cityStore.SaveAsync(new PlacedBuilding
        {
            ID = Guid.NewGuid(),
            BuildingId = buildingId,
            X = x,
            Y = y,
            PlacedAt = DateTimeOffset.UtcNow,
        });

        return PlaceResult.Ok(await GetCityAsync(), validation.CoinsDelta);
    }

    public async Task<PlaceResult> TryDemolishAsync(int x, int y)
    {
        var city = await GetCityAsync();
        var validation = CityEngine.ValidateDemolition(city, x, y);
        if (!validation.Success)
        {
            return validation;
        }

        var tile = city.GetTile(x, y)!;
        var placed = await cityStore.LoadAsync();
        var entity = placed.FirstOrDefault(p => p.ID == tile.PlacedBuildingId);
        if (entity is null)
        {
            return PlaceResult.Fail(PlaceError.TileEmpty);
        }

        await cityStore.DeleteAsync(entity);
        return PlaceResult.Ok(await GetCityAsync(), validation.CoinsDelta);
    }

    public async Task<PlaceResult> TryExpandAsync()
    {
        var city = await GetCityAsync();
        var validation = CityEngine.ValidateExpansion(city);
        if (!validation.Success)
        {
            return validation;
        }

        var step = city.NextExpansion!;
        await cityStore.SaveExpansionAsync(new CityExpansion
        {
            ID = Guid.NewGuid(),
            GridSize = step.GridSize,
            PurchasedAt = DateTimeOffset.UtcNow,
        });

        return PlaceResult.Ok(await GetCityAsync(), validation.CoinsDelta);
    }
}
