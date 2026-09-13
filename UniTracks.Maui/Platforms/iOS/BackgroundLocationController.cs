using CoreLocation;
using Foundation;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;

namespace UniTracks.Maui;

/// <summary>
/// iOS background location capture via CLLocationManager. The app is authorized "Always" before
/// recording starts; this manager keeps delivering updates when the app is backgrounded
/// (UIBackgroundModes=location is set in Info.plist).
///
/// iOS can end those updates without any error: the app is suspended, the system ends the update
/// session, or updates are held back and only delivered later. Nothing in the callbacks says so,
/// which is why a lost part of a trip used to be invisible. This controller therefore reports the
/// real authorization, logs every failure and gap (see <see cref="LocationDiagnostics"/>) and keeps
/// its delegate attached for a short drain window after <see cref="Stop"/>, so locations that iOS
/// still holds are not discarded by detaching too early.
/// </summary>
public class BackgroundLocationController : IBackgroundLocationController
{
    /// <summary>Gap between two fixes worth reporting. A moving run delivers about once per second.</summary>
    private const double GapWarningSeconds = 20;

    /// <summary>Heartbeat line every N fixes: keeps the log readable and proves that updates arrive.</summary>
    private const int HeartbeatEveryFixes = 100;

    private readonly IGpsDataStorageService storage;
    private readonly CLLocationManager locationManager;
    private readonly LocationDelegate locationDelegate;
    private readonly object gate = new();

    private int generation;
    private bool isRunning;
    private int fixCount;
    private double previousFixSeconds;
    private double longestGapSeconds;
    private DateTimeOffset? lastFixAt;

    public BackgroundLocationController(IGpsDataStorageService storage)
    {
        this.storage = storage;

        locationManager = new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBest,
            AllowsBackgroundLocationUpdates = true,
            PausesLocationUpdatesAutomatically = false,
            ActivityType = CLActivityType.Fitness,
            DistanceFilter = 0,

            // The system indicator is Apple's requirement while capturing in the background and the
            // only visible hint on the device when the background updates silently stop.
            ShowsBackgroundLocationIndicator = true
        };

        locationDelegate = new LocationDelegate(
            OnLocationReceived,
            OnAuthorizationChanged,
            OnFailed,
            OnDeferredUpdatesFinished);

        LocationDiagnostics.Write(
            $"iOS Controller bereit (Standortdienste systemweit aktiv: {CLLocationManager.LocationServicesEnabled}, " +
            $"Freigabe: {DescribeAuthorization(locationManager.AuthorizationStatus)}).");
    }

    public LocationCaptureHealth Health
    {
        get
        {
            lock (gate)
            {
                return LocationCaptureHealth.Tracked(
                    isRunning,
                    DescribeAuthorization(locationManager.AuthorizationStatus),
                    fixCount,
                    lastFixAt,
                    longestGapSeconds);
            }
        }
    }

    public void Start(Action<GPSInformatoion>? onUpdate)
    {
        locationManager.Delegate = locationDelegate;

        lock (gate)
        {
            generation++;
            isRunning = true;
            fixCount = 0;
            lastFixAt = null;
            previousFixSeconds = 0;
            longestGapSeconds = 0;
        }

        if (!CLLocationManager.LocationServicesEnabled)
        {
            LocationDiagnostics.Write("iOS START abgebrochen: Standortdienste sind systemweit deaktiviert.");
            StopRunningFlag();
            return;
        }

        CLAuthorizationStatus status = locationManager.AuthorizationStatus;
        LocationDiagnostics.Write($"iOS START angefordert, tatsächliche Freigabe: {DescribeAuthorization(status)}.");

        if (status is CLAuthorizationStatus.Denied or CLAuthorizationStatus.Restricted)
        {
            LocationDiagnostics.Write("iOS START abgebrochen: keine Standortfreigabe (Denied/Restricted).");
            StopRunningFlag();
            return;
        }

        if (status == CLAuthorizationStatus.AuthorizedWhenInUse)
        {
            // "When In Use" cannot capture in the background: iOS keeps the updates alive for a while
            // and then ends them without any error - exactly the pattern of a trip whose second half
            // is missing. Ask for the upgrade so the user sees the prompt, and say it in the log.
            LocationDiagnostics.Write(
                "iOS WARNUNG: Freigabe ist nur \"Beim Verwenden der App\". iOS beendet Hintergrund-Updates " +
                "nach einiger Zeit still -> der Rest der Strecke kann fehlen. Fordere \"Immer\" an.");
            locationManager.RequestAlwaysAuthorization();
        }

        locationManager.StartUpdatingLocation();
        LocationDiagnostics.Write("iOS StartUpdatingLocation() aufgerufen.");
    }

    public void Stop()
    {
        int token;
        int counted;
        double longest;
        DateTimeOffset? last;

        lock (gate)
        {
            isRunning = false;
            token = generation;
            counted = fixCount;
            longest = longestGapSeconds;
            last = lastFixAt;
        }

        locationManager.StopUpdatingLocation();

        LocationDiagnostics.Write(
            $"iOS StopUpdatingLocation(): {counted} Punkte in dieser Sitzung, " +
            $"letzter Punkt {(last is { } fix ? $"vor {(DateTimeOffset.Now - fix).TotalSeconds:F0} s" : "nie")}, " +
            $"größte Lücke {longest:F0} s. Delegate bleibt {LocationDrain.Grace.TotalSeconds:F0} s für Nachlieferungen aktiv.");

        ScheduleDetach(token);
    }

    private void StopRunningFlag()
    {
        lock (gate)
        {
            isRunning = false;
        }
    }

    private void ScheduleDetach(int token)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(LocationDrain.Grace).ConfigureAwait(false);
                DetachAfterDrain(token);
            }
            catch (Exception ex)
            {
                LocationDiagnostics.Write($"iOS Delegate-Detach fehlgeschlagen: {ex.Message}");
            }
        });
    }

    private void DetachAfterDrain(int token)
    {
        lock (gate)
        {
            // A new recording started in the meantime and installed the delegate again.
            if (generation != token)
            {
                return;
            }
        }

        void Detach()
        {
            try
            {
                // The binding declares the property non-nullable, but detaching is exactly what
                // Apple's Objective-C API expects here: nil simply means "no callbacks any more".
                locationManager.Delegate = null!;
                LocationDiagnostics.Write("iOS Location-Delegate nach der Nachliefer-Frist abgehängt.");
            }
            catch (Exception ex)
            {
                LocationDiagnostics.Write($"iOS Location-Delegate konnte nicht abgehängt werden: {ex.Message}");
            }
        }

        try
        {
            if (Microsoft.Maui.ApplicationModel.MainThread.IsMainThread)
            {
                Detach();
            }
            else
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(Detach);
            }
        }
        catch
        {
            // No MAUI main thread available (e.g. during shutdown): detaching directly is still
            // better than leaving a delegate that stores into a finished trip.
            Detach();
        }
    }

    private void OnLocationReceived(CLLocation location)
    {
        if (location is null)
        {
            return;
        }

        NoteFix(location);

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

    /// <summary>
    /// Counts fixes and measures the gap between two <em>fix</em> timestamps, so a suspension is
    /// reported even though the app only learns about it when the next (late) update arrives.
    /// </summary>
    private void NoteFix(CLLocation location)
    {
        double seconds = location.Timestamp.SecondsSinceReferenceDate;
        int counted;
        double gap = 0;

        lock (gate)
        {
            counted = ++fixCount;
            lastFixAt = DateTimeOffset.Now;

            if (previousFixSeconds > 0)
            {
                gap = seconds - previousFixSeconds;
                if (gap > longestGapSeconds)
                {
                    longestGapSeconds = gap;
                }
            }

            previousFixSeconds = seconds;
        }

        if (counted == 1)
        {
            LocationDiagnostics.Write(
                $"iOS erster Punkt: {location.Coordinate.Latitude:F6},{location.Coordinate.Longitude:F6}, " +
                $"Genauigkeit {location.HorizontalAccuracy:F1} m.");
        }
        else if (gap > GapWarningSeconds)
        {
            LocationDiagnostics.Write(
                $"iOS LÜCKE: {gap:F1} s ohne Standortpunkt (nach {counted} Punkten). iOS hat die App pausiert " +
                "oder die Zustellung zurückgehalten.");
        }

        if (counted % HeartbeatEveryFixes == 0)
        {
            LocationDiagnostics.Write($"iOS läuft: {counted} Punkte, größte Lücke {longestGapSeconds:F0} s.");
        }
    }

    private void OnAuthorizationChanged(CLAuthorizationStatus status)
    {
        LocationDiagnostics.Write($"iOS Freigabe geändert: {DescribeAuthorization(status)}.");

        if (status is CLAuthorizationStatus.Denied or CLAuthorizationStatus.Restricted)
        {
            LocationDiagnostics.Write("iOS: Freigabe entzogen -> es kommen keine Hintergrund-Updates mehr.");
        }
    }

    private void OnFailed(NSError? error)
    {
        string description = error is null
            ? "unbekannt"
            : $"{error.Domain}/{error.Code} {error.LocalizedDescription}";

        LocationDiagnostics.Write($"iOS FEHLER: {description}");

        if (error is not null && error.Code == (long)CLError.Denied)
        {
            // The documented case: an app without full authorization that keeps capturing in the
            // background is rejected with kCLErrorDenied and receives no further updates at all.
            LocationDiagnostics.Write("iOS FEHLER kCLErrorDenied: Hintergrundaufzeichnung abgelehnt.");
            StopRunningFlag();
        }
    }

    private void OnDeferredUpdatesFinished(NSError? error)
    {
        LocationDiagnostics.Write(
            $"iOS Nachlieferungen abgeschlossen{(error is null ? string.Empty : $": {error.LocalizedDescription}")}.");
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
            LocationDiagnostics.Write($"iOS Punkt konnte nicht gespeichert werden: {ex.GetType().Name} {ex.Message}");
        }
    }

    internal static string DescribeAuthorization(CLAuthorizationStatus status) => status switch
    {
        CLAuthorizationStatus.NotDetermined => "NotDetermined",
        CLAuthorizationStatus.Restricted => "Restricted",
        CLAuthorizationStatus.Denied => "Denied",
        CLAuthorizationStatus.AuthorizedAlways => "Immer",
        CLAuthorizationStatus.AuthorizedWhenInUse => "Beim Verwenden der App",
        _ => status.ToString()
    };

    private static DateTimeOffset ToDateTimeOffset(NSDate timestamp)
    {
        // NSDate's reference date is 2001-01-01T00:00:00Z.
        return new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero)
            .AddSeconds(timestamp.SecondsSinceReferenceDate);
    }

    private sealed class LocationDelegate : CLLocationManagerDelegate
    {
        private readonly Action<CLLocation> onLocation;
        private readonly Action<CLAuthorizationStatus> onAuthorization;
        private readonly Action<NSError> onFailed;
        private readonly Action<NSError?> onDeferredUpdatesFinished;

        public LocationDelegate(
            Action<CLLocation> onLocation,
            Action<CLAuthorizationStatus> onAuthorization,
            Action<NSError?> onFailed,
            Action<NSError?> onDeferredUpdatesFinished)
        {
            this.onLocation = onLocation;
            this.onAuthorization = onAuthorization;
            this.onFailed = onFailed;
            this.onDeferredUpdatesFinished = onDeferredUpdatesFinished;
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            foreach (CLLocation location in locations)
            {
                onLocation(location);
            }
        }

        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            => onAuthorization(status);

        // The C# name of the "locationManager:didFailWithError:" callback.
        public override void Failed(CLLocationManager manager, NSError error)
            => onFailed(error);

        public override void DeferredUpdatesFinished(CLLocationManager manager, NSError? error)
            => onDeferredUpdatesFinished(error);
    }
}
