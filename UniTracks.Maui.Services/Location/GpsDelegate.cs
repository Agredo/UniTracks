#if ANDROID
using AndroidX.Core.App;
#endif
using Shiny;
using Shiny.Locations;
using UniTracks.Models.GPS;
using UniTracks.Services.Location;

namespace UniTracks.Maui.Services.Location;

public partial class GpsDelegate : Shiny.Locations.IGpsDelegate
{
    public GpsDelegate()
    {

    }

    public async Task OnReading(GpsReading reading)
    {
        var latituede = reading.Position.Latitude;
        var longitude = reading.Position.Longitude;
        Console.WriteLine($"Latitude: {latituede}, Longitude: {longitude}");
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
