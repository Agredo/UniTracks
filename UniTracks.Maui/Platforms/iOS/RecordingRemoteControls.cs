using Foundation;
using UniTracks.Services.Location;
using UserNotifications;
using UIKit;

namespace UniTracks.Maui;

/// <summary>
/// iOS lock-screen controls for a running recording.
///
/// Two surfaces carry the same state, and the app uses whichever the device offers:
///
/// 1. A Live Activity (widget extension, see native/UniTracksActivityHost) draws a card on the lock
///    screen and in the Dynamic Island with a live clock and the buttons Pausieren/Fortsetzen and
///    Stoppen. The card is drawn by the extension, so a tap on it is handed over to this process by
///    the bridge (<see cref="LiveActivityBridge"/>). This is the surface the runner is meant to see.
///
/// 2. Where Live Activities are unavailable or switched off (Settings > UniTracks), a local
///    notification takes over: its category carries the same actions, reported back through
///    <see cref="UNUserNotificationCenterDelegate"/>.
///
/// Both are driven by <see cref="Update"/> and raise the same events, so the lock screen and the
/// in-app buttons act on the same recording. Only one of the two is ever shown - the card replaces
/// the notification instead of sitting next to it.
///
/// While the app is on screen it shows the state itself, so the notification is only posted while the
/// app is in the background and removed as soon as it comes back - no stale duplicate next to the app.
/// </summary>
public class RecordingRemoteControls : IRecordingRemoteControls
{
    private const string RunningCategory = "unitracks.recording.running";
    private const string PausedCategory = "unitracks.recording.paused";
    private const string PauseAction = "unitracks.recording.action.pause";
    private const string ResumeAction = "unitracks.recording.action.resume";
    private const string StopAction = "unitracks.recording.action.stop";
    private const string RequestIdentifier = "unitracks.recording";
    private const string ThreadIdentifier = "unitracks.recording";

    private readonly NotificationCenterDelegate centerDelegate = new();
    private RecordingRemoteState state = RecordingRemoteState.Stopped;
    private TimeSpan elapsed;
    private bool isAuthorized;
    private bool isLiveActivityShowing;

    public RecordingRemoteControls()
    {
        centerDelegate.ActionReceived += OnActionReceived;
        UNUserNotificationCenter.Current.Delegate = centerDelegate;

        RegisterCategories();

        // A tap on the card lands here instead of starting a second app instance: the extension has
        // the button, this process has the recording, and the bridge connects the two.
        LiveActivityBridge.SetCommandHandler(OnLiveActivityCommand);

        // The lock-screen surface belongs to the background: post it when the app leaves the screen,
        // drop it again when the runner is back in the app.
        NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.DidEnterBackgroundNotification,
            _ => Refresh(),
            null);
        NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillEnterForegroundNotification,
            _ => RemoveDelivered(),
            null);
    }

    public event EventHandler? PauseRequested;

    public event EventHandler? ResumeRequested;

    public event EventHandler? StopRequested;

    public void Update(RecordingRemoteState state, TimeSpan elapsed)
    {
        this.state = state;
        this.elapsed = elapsed;

        if (state == RecordingRemoteState.Stopped)
        {
            RemoveDelivered();
            EndLiveActivity();
            return;
        }

        PublishLiveActivity();

        if (UIApplication.SharedApplication.ApplicationState == UIApplicationState.Active)
        {
            // The recording page shows the state itself. iOS only presents the permission prompt while
            // the app is active, so this - the moment the recording starts - is where it is asked for.
            RequestAuthorization();
            return;
        }

        Refresh();
    }

    /// <summary>
    /// Pushes the state into the Live Activity. The first call creates the card, every later one
    /// updates it, so the card always describes the recording as of the last call. Without a card
    /// (Live Activities not allowed on this device or switched off for the app) this reports false and
    /// the notification below keeps working as before.
    /// </summary>
    private void PublishLiveActivity()
    {
        // The card derives its clock from the start timestamp: no update per second is needed, iOS
        // would throttle them anyway. Sending "now minus the time already recorded" keeps that clock
        // correct across pauses - after continuing it goes on where it stopped instead of including
        // the pause.
        isLiveActivityShowing = LiveActivityBridge.Publish(
            DateTimeOffset.UtcNow - elapsed,
            elapsed,
            state == RecordingRemoteState.Paused);
    }

    private void EndLiveActivity()
    {
        if (!isLiveActivityShowing)
        {
            return;
        }

        isLiveActivityShowing = false;
        LiveActivityBridge.End();
    }

    private void RegisterCategories()
    {
        var pause = UNNotificationAction.FromIdentifier(PauseAction, "Pausieren", UNNotificationActionOptions.None);
        var resume = UNNotificationAction.FromIdentifier(ResumeAction, "Fortsetzen", UNNotificationActionOptions.None);

        // Ending the recording opens the app, so the runner sees the finished trip and its statistics.
        var stop = UNNotificationAction.FromIdentifier(StopAction, "Stoppen", UNNotificationActionOptions.Foreground);

        UNUserNotificationCenter.Current.SetNotificationCategories(new NSSet<UNNotificationCategory>(
            UNNotificationCategory.FromIdentifier(
                RunningCategory, new[] { pause, stop }, Array.Empty<string>(), UNNotificationCategoryOptions.None),
            UNNotificationCategory.FromIdentifier(
                PausedCategory, new[] { resume, stop }, Array.Empty<string>(), UNNotificationCategoryOptions.None)));
    }

    private void RequestAuthorization()
    {
        UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
        {
            if (settings.AuthorizationStatus == UNAuthorizationStatus.Authorized
                || settings.AuthorizationStatus == UNAuthorizationStatus.Provisional)
            {
                isAuthorized = true;
                return;
            }

            if (settings.AuthorizationStatus != UNAuthorizationStatus.NotDetermined)
            {
                LocationDiagnostics.Write(
                    $"iOS Sperrbildschirm-Knoepfe: Mitteilungen sind nicht erlaubt ({settings.AuthorizationStatus}).");
                return;
            }

            // Sound is not requested: the lock-screen controls must never beep at the runner.
            UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert, (granted, error) =>
            {
                isAuthorized = granted;
                LocationDiagnostics.Write(
                    $"iOS Mitteilungs-Freigabe angefragt: erteilt={granted}{(error is null ? string.Empty : $", Fehler={error.LocalizedDescription}")}.");
            });
        });
    }

    private void Refresh()
    {
        if (state == RecordingRemoteState.Stopped)
        {
            return;
        }

        // The card on the lock screen already offers pause and stop, a second surface on top of it
        // would just be a duplicate.
        if (isLiveActivityShowing)
        {
            return;
        }

        var content = new UNMutableNotificationContent
        {
            Title = "UniTracks",
            Subtitle = state == RecordingRemoteState.Paused ? "Aufnahme pausiert" : "Aufnahme läuft",
            Body = state == RecordingRemoteState.Paused
                ? "Aufklappen zum Fortsetzen oder Beenden."
                : $"Aufklappen zum Pausieren oder Beenden. ({elapsed:hh\\:mm\\:ss})",

            // Silent: the control surface must not interrupt the runner.
            Sound = null,
            CategoryIdentifier = state == RecordingRemoteState.Paused ? PausedCategory : RunningCategory,
            ThreadIdentifier = ThreadIdentifier,
        };

        // No trigger: the notification is delivered right away. The fixed identifier replaces the
        // previous surface instead of stacking a second one per state change.
        var request = UNNotificationRequest.FromIdentifier(RequestIdentifier, content, null);
        UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
        {
            if (error is not null)
            {
                LocationDiagnostics.Write($"iOS Sperrbildschirm-Mitteilung fehlgeschlagen: {error.LocalizedDescription}");
            }
        });
    }

    private void RemoveDelivered()
    {
        UNUserNotificationCenter.Current.RemoveDeliveredNotifications(new[] { RequestIdentifier });
    }

    private void OnActionReceived(object? sender, string actionIdentifier)
    {
        switch (actionIdentifier)
        {
            case PauseAction:
                PauseRequested?.Invoke(this, EventArgs.Empty);
                break;

            case ResumeAction:
                ResumeRequested?.Invoke(this, EventArgs.Empty);
                break;

            case StopAction:
                StopRequested?.Invoke(this, EventArgs.Empty);
                break;

            default:
                // UNNotificationDefaultActionIdentifier: the user opened the notification, which simply
                // brings the app to the front. Nothing to do.
                break;
        }
    }

    /// <summary>
    /// A button of the Live Activity card was tapped. The tap comes from the system on a thread of its
    /// own, and the page switches to the main thread itself (see RunRemoteCommand in the view model),
    /// so only the command is passed on here.
    /// </summary>
    private void OnLiveActivityCommand(int command)
    {
        var name = command switch
        {
            LiveActivityBridge.PauseCommand => "Pausieren",
            LiveActivityBridge.ResumeCommand => "Fortsetzen",
            LiveActivityBridge.StopCommand => "Stoppen",
            _ => $"unbekannt ({command})",
        };

        // The card lives in the widget extension, so a tap that arrives here has crossed a process
        // boundary. Recorded because a tap that does *not* arrive leaves no other trace.
        LocationDiagnostics.Write($"iOS LiveActivity-Knopf: {name}.");

        switch (command)
        {
            case LiveActivityBridge.PauseCommand:
                PauseRequested?.Invoke(this, EventArgs.Empty);
                break;

            case LiveActivityBridge.ResumeCommand:
                ResumeRequested?.Invoke(this, EventArgs.Empty);
                break;

            case LiveActivityBridge.StopCommand:
                StopRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private sealed class NotificationCenterDelegate : UNUserNotificationCenterDelegate
    {
        public event EventHandler<string>? ActionReceived;

        public override void DidReceiveNotificationResponse(
            UNUserNotificationCenter center,
            UNNotificationResponse response,
            Action completionHandler)
        {
            ActionReceived?.Invoke(this, response.ActionIdentifier);
            completionHandler();
        }

        public override void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            // The app is in the foreground and shows the recording state itself, so the notification
            // stays quiet instead of covering the screen.
            completionHandler(UNNotificationPresentationOptions.None);
        }
    }
}
