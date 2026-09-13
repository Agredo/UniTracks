using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using AndroidLocation = Android.Locations.Location;

namespace UniTracks.Maui;

/// <summary>
/// Foreground service that keeps GPS recording alive while the app is in the background.
/// The [Service] attribute generates the manifest entry (incl. location foregroundServiceType)
/// with the correct Android Callable Wrapper name automatically.
///
/// The ongoing notification is the lock-screen surface for the recording: its "Pausieren",
/// "Fortsetzen" and "Stoppen" actions come back into <see cref="OnStartCommand"/> as intents, so
/// the runner can control the recording without unlocking the phone. The paused state is kept here
/// (the foreground service stays alive so the trip can continue) and is mirrored by the app.
/// </summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public class BackgroundLocationService : Service
{
    /// <summary>Action of the notification button that pauses the recording.</summary>
    public const string ActionPause = "unitracks.recording.PAUSE";

    /// <summary>Action of the notification button that continues a paused recording.</summary>
    public const string ActionResume = "unitracks.recording.RESUME";

    /// <summary>Action of the notification button that ends the recording.</summary>
    public const string ActionStop = "unitracks.recording.STOP";

    /// <summary>Action that carries the recording state the app holds into the notification.</summary>
    public const string ActionStateUpdate = "unitracks.recording.STATE";

    /// <summary>Extra of <see cref="ActionStateUpdate"/> holding a <see cref="RecordingRemoteState"/>.</summary>
    public const string ExtraState = "unitracks.recording.extra.STATE";

    /// <summary>Extra of <see cref="ActionStateUpdate"/> holding the already recorded milliseconds.</summary>
    public const string ExtraElapsedMs = "unitracks.recording.extra.ELAPSED_MS";

    private const string ChannelId = "unitracks.location";
    private const long MinTimeMs = 1000L;
    private const float MinDistanceMeters = 0f;
    private const int RequestCodeOpenApp = 1;
    private const int RequestCodePause = 2;
    private const int RequestCodeResume = 3;
    private const int RequestCodeStop = 4;

    /// <summary>Id of the ongoing recording notification, the app's lock-screen surface.</summary>
    public const int NotificationId = 0x554e4954; // 'UNIT'

    private LocationManager? locationManager;
    private LocationListener? locationListener;
    private IGpsDataStorageService? storage;
    private bool isForeground;
    private bool isUpdating;
    private bool isPaused;
    private long elapsedMs;

    public override void OnCreate()
    {
        base.OnCreate();

        storage = IPlatformApplication.Current?.Services?.GetService<IGpsDataStorageService>();
        locationManager = (LocationManager?)GetSystemService(Context.LocationService);
        locationListener = new LocationListener(OnLocationReceived);
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        switch (intent?.Action)
        {
            case ActionPause:
                SetPaused(true);
                return StartCommandResult.NotSticky;

            case ActionResume:
                SetPaused(false);
                return StartCommandResult.NotSticky;

            case ActionStop:
                RecordingRemoteCommands.Raise(RecordingRemoteCommand.Stop);
                StopCaptureAndService();
                return StartCommandResult.NotSticky;

            case ActionStateUpdate:
                int state = intent?.GetIntExtra(ExtraState, (int)RecordingRemoteState.Recording)
                    ?? (int)RecordingRemoteState.Recording;
                isPaused = state == (int)RecordingRemoteState.Paused;
                elapsedMs = intent?.GetLongExtra(ExtraElapsedMs, elapsedMs) ?? elapsedMs;
                SetPaused(isPaused);
                return StartCommandResult.Sticky;

            default:
                // A new recording, or a sticky restart with a null intent. Only a fresh service starts
                // the clock at zero - a service that is already running is continued (resume path).
                if (!isForeground)
                {
                    isPaused = false;
                    elapsedMs = 0L;
                }

                SetPaused(isPaused);
                return StartCommandResult.Sticky;
        }
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        StopLocationUpdates();
        base.OnDestroy();
    }

    private void SetPaused(bool paused)
    {
        bool changed = isPaused != paused;
        isPaused = paused;

        // While paused the foreground service keeps running but no provider is polled: the trip stays
        // open and the lock screen still offers "Fortsetzen" and "Stoppen".
        if (paused)
        {
            StopLocationUpdates();
        }
        else
        {
            StartLocationUpdates();
        }

        ShowNotification();

        // Only a real state change is reported: a fresh start also lands here with paused = false and
        // must not look like the user resumed a recording.
        if (changed)
        {
            RecordingRemoteCommands.Raise(paused ? RecordingRemoteCommand.Pause : RecordingRemoteCommand.Resume);
        }
    }

    private void StopCaptureAndService()
    {
        StopLocationUpdates();
        StopForeground(StopForegroundFlags.Remove);
        isForeground = false;
        StopSelf();
    }

    /// <summary>
    /// Shows the notification for the current state. A foreground service notification must exist
    /// within a few seconds of <c>StartForegroundService</c>, so an update that arrives on a service
    /// that is not foreground yet starts it; later updates refresh the existing notification.
    /// </summary>
    private void ShowNotification()
    {
        Notification notification = BuildNotification();

        if (isForeground)
        {
            NotificationManager? manager = (NotificationManager?)GetSystemService(Context.NotificationService);
            manager?.Notify(NotificationId, notification);
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        isForeground = true;

        // Without the notification permission (Android 13+) the system hides the whole notification,
        // so the lock-screen buttons are missing even though the recording runs.
        if (!AndroidX.Core.App.NotificationManagerCompat.From(this).AreNotificationsEnabled())
        {
            System.Diagnostics.Debug.WriteLine(
                "[UniTracks] Ohne Mitteilungs-Freigabe zeigt Android die Aufnahme-Mitteilung und damit die " +
                "Sperrbildschirm-Knoepfe nicht an.");
        }
    }

    private Notification BuildNotification()
    {
        EnsureChannel();

        Notification.Builder builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? new Notification.Builder(this, ChannelId)
            : new Notification.Builder(this);

        builder
            .SetContentTitle("UniTracks")
            .SetContentText(isPaused ? "Aufnahme pausiert" : "Aufnahme läuft")
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetVisibility(NotificationVisibility.Public)
            .SetContentIntent(CreateOpenAppIntent());

        if (isPaused)
        {
            AddAction(builder, Android.Resource.Drawable.IcMediaPlay, "Fortsetzen", ActionResume, RequestCodeResume);
        }
        else
        {
            // The chronometer keeps counting on the lock screen without the app pushing updates.
            builder.SetUsesChronometer(true).SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis() - elapsedMs);
            AddAction(builder, Android.Resource.Drawable.IcMediaPause, "Pausieren", ActionPause, RequestCodePause);
        }

        AddAction(builder, Android.Resource.Drawable.IcMenuCloseClearCancel, "Stoppen", ActionStop, RequestCodeStop);

        return builder.Build();
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        NotificationManager? manager = (NotificationManager?)GetSystemService(Context.NotificationService);
        var channel = new NotificationChannel(ChannelId, "Standort-Aufnahme", NotificationImportance.Low)
        {
            Description = "Hält die GPS-Aufnahme am Laufen, während die App im Hintergrund ist."
        };
        manager?.CreateNotificationChannel(channel);
    }

    private void AddAction(Notification.Builder builder, int icon, string title, string action, int requestCode)
    {
        PendingIntent? pendingIntent = CreateServiceIntent(action, requestCode);

        if (pendingIntent is null)
        {
            return;
        }

        Android.Graphics.Drawables.Icon actionIcon = Android.Graphics.Drawables.Icon.CreateWithResource(this, icon);
        builder.AddAction(new Notification.Action.Builder(actionIcon, title, pendingIntent).Build());
    }

    private PendingIntent? CreateServiceIntent(string action, int requestCode)
    {
        var intent = new Intent(this, typeof(BackgroundLocationService));
        intent.SetAction(action);

        // Immutable: the buttons carry no mutable payload, and Android 14 rejects mutable ones.
        return PendingIntent.GetService(
            this,
            requestCode,
            intent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
    }

    private PendingIntent? CreateOpenAppIntent()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop);

        return PendingIntent.GetActivity(
            this,
            RequestCodeOpenApp,
            intent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
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
