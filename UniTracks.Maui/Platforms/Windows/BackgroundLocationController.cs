using IGeolocation = AgredoApplication.MVVM.Services.Abstractions.Devices.IGeolocation;
using GeolocationRequest = AgredoApplication.MVVM.Services.Models.Devices.GeolocationRequest;
using GeolocationAccuracy = AgredoApplication.MVVM.Services.Models.Devices.GeolocationAccuracy;
using SharedLocation = AgredoApplication.MVVM.Services.Models.Devices.Location;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;

namespace UniTracks.Maui;

/// <summary>
/// Windows fallback. Desktop keeps the same get-location polling behaviour as before; a
/// foreground/background service is not required on the desktop, so we just keep the loop.
/// </summary>
public class BackgroundLocationController : IBackgroundLocationController
{
    private readonly IGeolocation geolocation;
    private readonly IGpsDataStorageService storage;
    private CancellationTokenSource? listeningCts;

    public BackgroundLocationController(IGeolocation geolocation, IGpsDataStorageService storage)
    {
        this.geolocation = geolocation;
        this.storage = storage;
    }

    public void Start(Action<GPSInformatoion>? onUpdate)
    {
        _ = StartListeningCoreAsync(onUpdate);
    }

    public void Stop()
    {
        listeningCts?.Cancel();
        listeningCts?.Dispose();
        listeningCts = null;
    }

    private async Task StartListeningCoreAsync(Action<GPSInformatoion>? onUpdate)
    {
        Stop();

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
                    onUpdate?.Invoke(information);
                    await storage.StoreData(information);
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
