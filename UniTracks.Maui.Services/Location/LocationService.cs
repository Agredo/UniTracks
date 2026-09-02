using IGeolocation = AgredoApplication.MVVM.Services.Abstractions.Devices.IGeolocation;
using GeolocationRequest = AgredoApplication.MVVM.Services.Models.Devices.GeolocationRequest;
using GeolocationAccuracy = AgredoApplication.MVVM.Services.Models.Devices.GeolocationAccuracy;
using SharedLocation = AgredoApplication.MVVM.Services.Models.Devices.Location;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;

namespace UniTracks.Maui.Services.Location;

public class LocationService : ILocationService
{
    private readonly IGeolocation geolocation;
    private readonly IGpsDataStorageService gpsDataStorageService;
    private CancellationTokenSource? listeningCts;

    public LocationService(IGeolocation geolocation, IGpsDataStorageService gpsDataStorageService)
    {
        this.geolocation = geolocation;
        this.gpsDataStorageService = gpsDataStorageService;
    }

    public Task StartListening(Action<GPSInformatoion> action) => StartListeningCoreAsync(action);

    public Task StartListening() => StartListeningCoreAsync(null);

    public void StopListening()
    {
        listeningCts?.Cancel();
        listeningCts?.Dispose();
        listeningCts = null;
    }

    private async Task StartListeningCoreAsync(Action<GPSInformatoion>? action)
    {
        StopListening();

        var cts = new CancellationTokenSource();
        listeningCts = cts;

        var request = new GeolocationRequest
        {
            DesiredAccuracy = GeolocationAccuracy.Best,
            Timeout = TimeSpan.FromSeconds(10)
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                SharedLocation? location = await geolocation.GetLocationAsync(request);
                if (location is not null)
                {
                    var information = ToGpsInformation(location);
                    action?.Invoke(information);
                    await gpsDataStorageService.StoreData(information);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Listener was stopped.
        }
    }

    private static GPSInformatoion ToGpsInformation(SharedLocation location)
    {
        return new GPSInformatoion(
            new Position(location.Longitude, location.Latitude),
            location.Accuracy ?? 0,
            location.Timestamp,
            location.Course ?? 0,
            0,
            location.Altitude ?? 0,
            location.Speed ?? 0,
            0);
    }
}
