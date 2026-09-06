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
}
