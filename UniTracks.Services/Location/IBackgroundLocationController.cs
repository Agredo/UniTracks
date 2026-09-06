using UniTracks.Models.GPS;

namespace UniTracks.Services.Location;

/// <summary>
/// Platform-specific background location capture. Implementations keep recording while the app
/// is not in the foreground (Android foreground service, iOS CLLocationManager). The change is
/// deliberately abstracted so the app layer stays platform-agnostic.
/// </summary>
public interface IBackgroundLocationController
{
    /// <summary>Starts continuous location capture. <paramref name="onUpdate"/> is optional.</summary>
    void Start(Action<GPSInformatoion>? onUpdate);

    /// <summary>Stops location capture and releases resources.</summary>
    void Stop();
}
