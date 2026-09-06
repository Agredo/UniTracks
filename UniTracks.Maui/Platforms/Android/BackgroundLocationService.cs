using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using AndroidLocation = Android.Locations.Location;

namespace UniTracks.Maui;

/// <summary>
/// Foreground service that keeps GPS recording alive while the app is in the background.
/// The [Service] attribute generates the manifest entry (incl. location foregroundServiceType)
/// with the correct Android Callable Wrapper name automatically.
/// </summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public class BackgroundLocationService : Service
{
    private const string ChannelId = "unitracks.location";
    private const int NotificationId = 0x554e4954; // 'UNIT'
    private const long MinTimeMs = 1000L;
    private const float MinDistanceMeters = 0f;

    private LocationManager? locationManager;
    private LocationListener? locationListener;
    private IGpsDataStorageService? storage;
    private bool isForeground;
    private bool isUpdating;

    public override void OnCreate()
    {
        base.OnCreate();

        storage = IPlatformApplication.Current?.Services?.GetService<IGpsDataStorageService>();
        locationManager = (LocationManager?)GetSystemService(Context.LocationService);
        locationListener = new LocationListener(OnLocationReceived);
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        StartForegroundWithNotification();
        StartLocationUpdates();
        // Sticky: if the system kills us, it restarts us with the last intent.
        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        StopLocationUpdates();
        base.OnDestroy();
    }

    private void StartForegroundWithNotification()
    {
        if (isForeground)
        {
            return;
        }

        NotificationManager? manager = (NotificationManager?)GetSystemService(Context.NotificationService);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Standort-Aufnahme", NotificationImportance.Low)
            {
                Description = "Hält die GPS-Aufnahme am Laufen, während die App im Hintergrund ist."
            };
            manager?.CreateNotificationChannel(channel);
        }

        Notification.Builder builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? new Notification.Builder(this, ChannelId)
            : new Notification.Builder(this);

        Notification notification = builder
            .SetContentTitle("UniTracks")
            .SetContentText("Aufnahme läuft")
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .Build();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }
        isForeground = true;
    }

    private void StartLocationUpdates()
    {
        if (isUpdating || locationManager is null || locationListener is null)
        {
            return;
        }

        try
        {
            // GPS first; emulators/indoors may not expose a GPS provider, so fall through.
            locationManager.RequestLocationUpdates(
                LocationManager.GpsProvider, MinTimeMs, MinDistanceMeters, locationListener, Looper.MainLooper);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UniTracks] GPS provider unavailable: {ex.Message}");
        }

        try
        {
            locationManager.RequestLocationUpdates(
                LocationManager.NetworkProvider, MinTimeMs, MinDistanceMeters, locationListener, Looper.MainLooper);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UniTracks] Network provider unavailable: {ex.Message}");
        }

        isUpdating = true;
    }

    private void StopLocationUpdates()
    {
        locationManager?.RemoveUpdates(locationListener);
        isUpdating = false;
    }

    private void OnLocationReceived(AndroidLocation location)
    {
        if (location is null)
        {
            return;
        }

        var information = new GPSInformatoion(
            new Position(location.Longitude, location.Latitude),
            location.Accuracy,
            DateTimeOffset.FromUnixTimeMilliseconds(location.Time),
            location.Bearing,
            0,
            location.Altitude,
            location.Speed,
            0);

        _ = HandleLocationAsync(information);
    }

    private async Task HandleLocationAsync(GPSInformatoion information)
    {
        if (storage is null)
        {
            return;
        }

        try
        {
            await storage.StoreData(information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UniTracks] Background location store failed: {ex}");
        }
    }

    private sealed class LocationListener : Java.Lang.Object, ILocationListener
    {
        private readonly Action<AndroidLocation> onLocation;

        public LocationListener(Action<AndroidLocation> onLocation)
        {
            this.onLocation = onLocation;
        }

        public void OnLocationChanged(AndroidLocation location)
        {
            onLocation(location);
        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string provider, Availability status, Bundle? extras)
        {
        }
    }
}
