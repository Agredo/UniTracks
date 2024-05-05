#if ANDROID
using AndroidX.Core.App;
#endif
using Shiny;
using Shiny.Locations;
using System.Reactive.Linq;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;

namespace UniTracks.Maui.Services.Location;

public partial class GpsDelegate : Shiny.Locations.IGpsDelegate
{
    public IGpsDataStorageService GpsDataStorageService { get; }
    public GpsDelegate(IGpsDataStorageService gpsDataStorageService)
    {
        GpsDataStorageService = gpsDataStorageService;
    }

    public async Task OnReading(GpsReading reading)
    {
        var GPSInformatoion = new GPSInformatoion(new Models.GPS.Position(reading.Position.Latitude, reading.Position.Longitude),
            reading.PositionAccuracy,
            reading.Timestamp,
            reading.Heading,
            reading.HeadingAccuracy,
            reading.Altitude,
            reading.Speed,
            reading.SpeedAccuracy);

        await GpsDataStorageService.StoreData(GPSInformatoion);
    }
}

#if ANDROID
public partial class GpsDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(NotificationCompat.Builder builder)
    {
        builder
                .SetContentTitle("MyApp")
                .SetContentText("My App is following you!! images");
        //.SetSmallIcon(Resource.Mipmap.youricon);
    }
}
#endif
