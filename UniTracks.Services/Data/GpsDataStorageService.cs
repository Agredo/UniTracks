using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.GPS;
using UniTracks.Models.Trip;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Data;

public class GpsDataStorageService : IGpsDataStorageService
{
    private Trip? currentTrip;

    public GpsDataStorageService(
        IGenericRepository<SqliteDBContext> sqliteRepository,
        IGenericLiteDBRepository<ILiteDatabase> liteDBRepository)
    {
        SqliteRepository = sqliteRepository;
        LiteDBRepository = liteDBRepository;
    }

    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public IGenericLiteDBRepository<ILiteDatabase> LiteDBRepository { get; }

    public async Task<List<LocationModel>> getAll()
    {
        return (await SqliteRepository.GetAllAsync<LocationModel>()).ToList();
    }

    public async Task StoreData(GPSInformatoion gpsInformatoion, Action<GPSInformatoion> action)
    {
        var latitude = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;
        Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");
        action(gpsInformatoion);
    }

    public async Task StoreData(GPSInformatoion gpsInformatoion)
    {
        var latitude = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;

        LocationModel location = new LocationModel()
        {
            Latitude = latitude,
            Longitude = longitude,
            Altitude = gpsInformatoion.Altitude,
            Accuracy = gpsInformatoion.PositionAccuracy,
            Speed = gpsInformatoion.Speed,
            Heading = gpsInformatoion.Heading,
            HeadingAccuracy = gpsInformatoion.HeadingAccuracy,
            SpeedAccuracy = gpsInformatoion.SpeedAccuracy,
            Timestamp = gpsInformatoion.Timestamp
        };

        if (currentTrip != null)
        {
            currentTrip.Locations.Add(location);
            currentTrip.MaxSpeed = currentTrip.Locations.Max(x => x.Speed);
            currentTrip.MinSpeed = currentTrip.Locations.Min(x => x.Speed);
            currentTrip.MaxAltitude = currentTrip.Locations.Max(x => x.Altitude);
            currentTrip.MinAltitude = currentTrip.Locations.Min(x => x.Altitude);
            currentTrip.MaxHeading = currentTrip.Locations.Max(x => x.Heading);
            currentTrip.MinHeading = currentTrip.Locations.Min(x => x.Heading);
            currentTrip.MaxAccuracy = currentTrip.Locations.Max(x => x.Accuracy);
            currentTrip.MinAccuracy = currentTrip.Locations.Min(x => x.Accuracy);
            currentTrip.AverageSpeed = currentTrip.Locations.Average(x => x.Speed);

            currentTrip.Distance = CalculateDistance(currentTrip.Locations);

            await SqliteRepository.Update<Trip>(currentTrip);
        }
        else
        {
            var trip = new Trip()
            {
                StartTime = DateTimeOffset.Now,
                Locations = new List<LocationModel>() { location },
                MaxSpeed = location.Speed,
                MinSpeed = location.Speed,
                MaxAltitude = location.Altitude,
                MinAltitude = location.Altitude,
                MaxHeading = location.Heading,
                MinHeading = location.Heading,
                MaxAccuracy = location.Accuracy,
                MinAccuracy = location.Accuracy
            };

            currentTrip = await SqliteRepository.Add<Trip>(trip);
        }

        Console.WriteLine($"CurrentTrip: {currentTrip.ID} {currentTrip.StartTime} Latitude: {currentTrip.Locations.Last().Latitude}, Longitude: {currentTrip.Locations.Last().Longitude}");

        await LiteDBRepository.Add<LocationModel>(location);
    }

    private static double CalculateDistance(List<LocationModel> locations)
    {
        double distance = 0;
        for (int i = 0; i < locations.Count - 1; i++)
        {
            distance += HaversineDistance(locations[i], locations[i + 1]);
        }

        return distance;
    }

    /// <summary>Distance between two coordinates in meters, using the Haversine formula.</summary>
    private static double HaversineDistance(LocationModel location1, LocationModel location2)
    {
        const double earthRadius = 6371000;

        double dLat = ToRadians(location2.Latitude - location1.Latitude);
        double dLon = ToRadians(location2.Longitude - location1.Longitude);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(location1.Latitude)) * Math.Cos(ToRadians(location2.Latitude)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadius * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
