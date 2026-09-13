using Android.App;
using Android.Content;
using UniTracks.Models.GPS;
using UniTracks.Services.Location;

namespace UniTracks.Maui;

/// <summary>
/// Starts/stops the Android foreground location service. The app is foreground when the user
/// taps record, so starting the service is allowed; once running it keeps capturing in the
/// background with a persistent notification. That notification is also the lock-screen surface for
/// the recording, which is why pausing talks to the service instead of tearing it down.
/// </summary>
public class BackgroundLocationController : IBackgroundLocationController
{
    public void Start(Action<GPSInformatoion>? onUpdate)
    {
        Context context = Android.App.Application.Context;
        Intent intent = new(context, typeof(BackgroundLocationService));
        BackgroundLocationServiceLauncher.Send(context, intent);
        LocationDiagnostics.Write("Android StartForegroundService(BackgroundLocationService) aufgerufen.");
    }

    public void Stop()
    {
        Context context = Android.App.Application.Context;
        Intent intent = new(context, typeof(BackgroundLocationService));
        context.StopService(intent);
        LocationDiagnostics.Write("Android StopService(BackgroundLocationService) aufgerufen.");
    }

    /// <summary>
    /// Pauses by telling the running foreground service to stop polling the location providers. The
    /// service stays alive (and keeps its notification with "Fortsetzen"/"Stoppen"), so the trip can
    /// be continued from the lock screen and no new service has to be started for the resume.
    /// </summary>
    public void Pause()
    {
        Context context = Android.App.Application.Context;
        Intent intent = new(context, typeof(BackgroundLocationService));
        intent.SetAction(BackgroundLocationService.ActionPause);
        BackgroundLocationServiceLauncher.Send(context, intent);
        LocationDiagnostics.Write("Android Pause an BackgroundLocationService gesendet.");
    }
}
