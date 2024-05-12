using GeoCoordinatePortable;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.GPS;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Services.Data;

namespace UniTracks.Core.Services;

public class GpsDataStorageService : IGpsDataStorageService
{
    private Trip currentTrip;


    public GpsDataStorageService(IGenericRepository<SqliteDBContext> sqliteRepository, IGenericLiteDBRepository<ILiteDatabase> liteDBRepository)
    {
        SqliteRepository = sqliteRepository;
        LiteDBRepository = liteDBRepository;
    }

    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public IGenericLiteDBRepository<ILiteDatabase> LiteDBRepository { get; }

    public async Task<List<Location>> getAll()
    {
        return (await SqliteRepository.GetAllAsync<Location>()).ToList();
    }

    public async Task StoreData(GPSInformatoion gpsInformatoion, Action<GPSInformatoion> action)
    {
        var latituede = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;
        Console.WriteLine($"Latitude: {latituede}, Longitude: {longitude}");
        action(gpsInformatoion);
    }

    public async Task StoreData(GPSInformatoion gpsInformatoion)
    {
        var latituede = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;

        Models.Location.Location location = new Models.Location.Location()
        {
            Latitude = latituede,
            Longitude = longitude,
            Altitude = gpsInformatoion.Altitude,
            Speed = gpsInformatoion.Speed,
            Heading = gpsInformatoion.Heading,
            HeadingAccuracy = gpsInformatoion.HeadingAccuracy,
            SpeedAccuracy = gpsInformatoion.SpeedAccuracy,
            Timestamp = gpsInformatoion.Timestamp
        };

        if(currentTrip != null)
        {
            currentTrip.Locations.Add(location);
            currentTrip.MaxSpeed = currentTrip.Locations.Max(x => x.Speed);
            currentTrip.MinSpeed = currentTrip.Locations.Min(x => x.Speed);
            currentTrip.MaxAltitude = currentTrip.Locations.Max(x => x.Altitude);
            currentTrip.MinAltitude = currentTrip.Locations.Min(x => x.Altitude);
            currentTrip.MaxHeading = currentTrip.Locations.Max(x => x.Heading);
            currentTrip.MinHeading = currentTrip.Locations.Min(x => x.Heading);
            currentTrip.AverageSpeed = currentTrip.Locations.Average(x => x.Speed);

            currentTrip.Distance = calculateDistance(currentTrip.Locations);

            await SqliteRepository.Update<Trip>(currentTrip);
        }
        else
        {
            var trip = new Trip()
            {
                StartTime = DateTime.Now,
                Locations = new List<Location>() { location },
                MaxSpeed = location.Speed,
                MinSpeed = location.Speed,
                MaxAltitude = location.Altitude,
                MinAltitude = location.Altitude,
                MaxHeading = location.Heading,
                MinHeading = location.Heading
            };

            currentTrip = await SqliteRepository.Add<Trip>(trip);
        }

        Console.WriteLine($"CurrentTrip: {currentTrip.ID} {currentTrip.StartTime} Latitude: {currentTrip.Locations.Last().Latitude}, Longitude: {currentTrip.Locations.Last().Latitude}");

        await LiteDBRepository.Add<Location>(location);
    }

    private double calculateDistance(List<Location> locations)
    {
        double distance = 0;
        for (int i = 0; i < locations.Count - 1; i++)
        {
            var location1 = locations[i];
            var location2 = locations[i + 1];
            GeoCoordinate coordinate1 = new GeoCoordinate(location1.Latitude, location1.Longitude);
            GeoCoordinate coordinate2 = new GeoCoordinate(location2.Latitude, location2.Longitude);

            distance += coordinate1.GetDistanceTo(coordinate2);
        }

        return distance;
    }
}
