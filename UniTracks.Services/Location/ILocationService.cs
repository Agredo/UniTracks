using UniTracks.Models.GPS;

namespace UniTracks.Services.Location;

public interface ILocationService
{
    public Task StartListening(Action<GPSInformatoion> action);
    public Task StartListening();
    public void StopListening();

    /// <summary>
    /// Suspends capture but keeps the platform session, so a paused recording can be continued with
    /// <see cref="StartListening()"/> without the platform tearing its session down.
    /// </summary>
    public void PauseListening();

    /// <summary>
    /// Stops capture and waits for the platform's drain window, so locations that iOS queued while
    /// the app was suspended are still stored before the trip is finalised.
    /// </summary>
    public Task StopListeningAndDrainAsync();

    /// <summary>Current platform capture health (authorization, fixes, gaps).</summary>
    public LocationCaptureHealth Health => LocationCaptureHealth.Unknown;
}

