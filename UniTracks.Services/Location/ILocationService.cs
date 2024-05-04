using UniTracks.Models.GPS;

namespace UniTracks.Services.Location;

public interface ILocationService
{
    public Task StartListening(Action<GPSInformatoion> action);
    public void StopListening();
}

