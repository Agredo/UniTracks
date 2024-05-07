using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;

namespace UniTracks.Core.Services;

public class GpsDataStorageService : IGpsDataStorageService
{

    public GpsDataStorageService(IGenericRepository<SqliteDBContext> sqliteRepository)
    {
        SqliteRepository = sqliteRepository;
    }

    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }

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
        Console.WriteLine($"Latitude: {latituede}, Longitude: {longitude}");

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

        await SqliteRepository.Context.Locations.AddAsync(location);
    }
}
