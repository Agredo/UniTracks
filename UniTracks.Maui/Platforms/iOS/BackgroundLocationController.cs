using CoreLocation;
using Foundation;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;

namespace UniTracks.Maui;

/// <summary>
/// iOS/MacCatalyst background location capture via CLLocationManager. The app is authorized
/// "Always" before recording starts; this manager keeps delivering updates when the app is
/// backgrounded (UIBackgroundModes=location is set in Info.plist).
/// </summary>
public class BackgroundLocationController : IBackgroundLocationController
{
    private readonly IGpsDataStorageService storage;
    private readonly CLLocationManager locationManager;
    private readonly LocationDelegate locationDelegate;

    public BackgroundLocationController(IGpsDataStorageService storage)
    {
        this.storage = storage;

        locationManager = new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBest,
            AllowsBackgroundLocationUpdates = true,
            PausesLocationUpdatesAutomatically = false,
            ActivityType = CLActivityType.Fitness,
            DistanceFilter = 0
        };

        locationDelegate = new LocationDelegate(OnLocationReceived);
    }

    public void Start(Action<GPSInformatoion>? onUpdate)
    {
        locationManager.Delegate = locationDelegate;

        if (CLLocationManager.LocationServicesEnabled)
        {
            locationManager.StartUpdatingLocation();
        }
    }

    public void Stop()
    {
        locationManager.StopUpdatingLocation();
        locationManager.Delegate = null;
    }

    private void OnLocationReceived(CLLocation location)
    {
        if (location is null)
        {
            return;
        }

        var information = new GPSInformatoion(
            new Position(location.Coordinate.Longitude, location.Coordinate.Latitude),
            location.HorizontalAccuracy,
            ToDateTimeOffset(location.Timestamp),
            location.Course,
            0,
            location.Altitude,
            location.Speed,
            0);

        _ = StoreAsync(information);
    }

    private async Task StoreAsync(GPSInformatoion information)
    {
        try
        {
            await storage.StoreData(information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UniTracks] iOS location store failed: {ex}");
        }
    }

    private static DateTimeOffset ToDateTimeOffset(NSDate timestamp)
    {
        // NSDate's reference date is 2001-01-01T00:00:00Z.
        return new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero)
            .AddSeconds(timestamp.SecondsSinceReferenceDate);
    }

    private sealed class LocationDelegate : CLLocationManagerDelegate
    {
        private readonly Action<CLLocation> onLocation;

        public LocationDelegate(Action<CLLocation> onLocation)
        {
            this.onLocation = onLocation;
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            foreach (CLLocation location in locations)
            {
                onLocation(location);
            }
        }
    }
}
