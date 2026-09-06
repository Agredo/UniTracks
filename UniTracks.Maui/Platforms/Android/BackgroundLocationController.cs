using Android.App;
using Android.Content;
using AndroidX.Core.Content;
using UniTracks.Models.GPS;
using UniTracks.Services.Location;

namespace UniTracks.Maui;

/// <summary>
/// Starts/stops the Android foreground location service. The app is foreground when the user
/// taps record, so starting the service is allowed; once running it keeps capturing in the
/// background with a persistent notification.
/// </summary>
public class BackgroundLocationController : IBackgroundLocationController
{
    public void Start(Action<GPSInformatoion>? onUpdate)
    {
        Context context = Android.App.Application.Context;
        Intent intent = new(context, typeof(BackgroundLocationService));
        ContextCompat.StartForegroundService(context, intent);
    }

    public void Stop()
    {
        Context context = Android.App.Application.Context;
        Intent intent = new(context, typeof(BackgroundLocationService));
        context.StopService(intent);
    }
}
