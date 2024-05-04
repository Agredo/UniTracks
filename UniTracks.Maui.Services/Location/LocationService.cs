using Shiny.Locations;
using UniTracks.Models.GPS;
using UniTracks.Services.Location;

namespace UniTracks.Maui.Services.Location
{
    public class LocationService(IGpsManager gpsManager) : ILocationService
    {
        public IGpsManager GpsManager { get; } = gpsManager;

        public async Task StartListening(Action<GPSInformatoion> action)
        {
            await GpsManager.StartListener(new GpsRequest(GpsBackgroundMode.Realtime, GpsAccuracy.Highest));

            var subscription = GpsManager
                .WhenReading()
                .Subscribe(reading =>
                {
                    var GPSInformatoion = new GPSInformatoion(
                                               new Models.GPS.Position(reading.Position.Latitude, reading.Position.Longitude),
                                                   reading.PositionAccuracy,
                                                   reading.Timestamp,
                                                   reading.Heading,
                                                   reading.HeadingAccuracy,
                                                   reading.Altitude,
                                                   reading.Speed,
                                                   reading.SpeedAccuracy
                                               );
                    action(GPSInformatoion);
                });
        }

        public void StopListening()
        {
            GpsManager.StopListener();
        }
    }
}
