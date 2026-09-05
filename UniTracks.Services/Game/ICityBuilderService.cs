using UniTracks.Games.CityBuilder;

namespace UniTracks.Services.Game;

/// <summary>Use-case service for the cozy city builder: load the city, place and demolish buildings.</summary>
public interface ICityBuilderService
{
    /// <summary>Rebuilds the current city state (tiles + coin balance) from persistence and activity.</summary>
    Task<CityState> GetCityAsync();

    /// <summary>Places a building on the given tile. Fails (no persistence) when invalid or unaffordable.</summary>
    Task<PlaceResult> TryPlaceAsync(string buildingId, int x, int y);

    /// <summary>Demolishes the building on the given tile and refunds part of its cost.</summary>
    Task<PlaceResult> TryDemolishAsync(int x, int y);

    /// <summary>Buys the next city-grid expansion (level-gated, coin-priced).</summary>
    Task<PlaceResult> TryExpandAsync();
}
