using UniTracks.Models.GPS;
using UniTracks.Services.Location;

namespace UniTracks.Maui.Services.Location;

/// <summary>
/// Thin platform-neutral coordinator. The platform-specific background capture lives in
/// <see cref="IBackgroundLocationController"/>, keeping Android/iOS concerns out of the app layer.
/// </summary>
public class LocationService : ILocationService
{
    private readonly IBackgroundLocationController controller;

    public LocationService(IBackgroundLocationController controller)
    {
        this.controller = controller;
    }

    public Task StartListening(Action<GPSInformatoion> action)
    {
        controller.Start(action);
        return Task.CompletedTask;
    }

    public Task StartListening()
    {
        controller.Start(null);
        return Task.CompletedTask;
    }

    public void StopListening()
    {
        controller.Stop();
    }

    /// <summary>
    /// Stops capture and waits for the drain window before returning. The platform controller keeps
    /// its delegate attached for the same window, so locations that iOS still holds - typically the
    /// part of the trip that was recorded while the app was suspended - are stored before the caller
    /// finalises the trip. Without the wait those points arrived after the trip was already closed
    /// and were dropped, which is exactly how the way back to the start disappeared.
    /// </summary>
    public async Task StopListeningAndDrainAsync()
    {
        controller.Stop();

        if (LocationDrain.Grace <= TimeSpan.Zero)
        {
            return;
        }

        LocationDiagnostics.Write(
            $"Warte {LocationDrain.Grace.TotalSeconds:F0} s auf Standort-Nachlieferungen der Plattform.");

        await Task.Delay(LocationDrain.Grace).ConfigureAwait(false);
    }

    public LocationCaptureHealth Health => controller.Health;
}
