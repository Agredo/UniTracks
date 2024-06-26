﻿using Shiny.Locations;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;
using UniTracks.Services.Location;

namespace UniTracks.Maui.Services.Location
{
    public class LocationService : ILocationService
    {
        public IGpsManager GpsManager { get; }
        public IGpsDataStorageService GpsDataStorageService { get; }

#if WINDOWS
        public LocationService()
        {

        }
#endif
        public LocationService(IGpsManager gpsManager, IGpsDataStorageService gpsDataStorageService)
        {
            GpsManager = gpsManager;
            GpsDataStorageService = gpsDataStorageService;
        }

        public async Task StartListening(Action<GPSInformatoion> action)
        {
            await GpsManager?.StartListener(new GpsRequest(GpsBackgroundMode.Realtime, GpsAccuracy.Highest) );

            var subscription = GpsManager
                .WhenReading()
                .Subscribe(reading =>
                {
                    GPSInformatoion gpsInformatoion = new GPSInformatoion(new Models.GPS.Position(reading.Position.Latitude, reading.Position.Longitude),
                                               reading.PositionAccuracy,
                                               reading.Timestamp,
                                               reading.Heading,
                                               reading.HeadingAccuracy,
                                               reading.Altitude,
                                               reading.Speed,
                                               reading.SpeedAccuracy);

                    GpsDataStorageService?.StoreData(gpsInformatoion);
                });
        }

        public async Task StartListening()
        {
            try
            {
                await GpsManager?.StartListener(new GpsRequest(GpsBackgroundMode.Realtime, GpsAccuracy.Highest));

                var subscription = GpsManager
                    .WhenReading()
                    .Subscribe(reading =>
                    {
                        GPSInformatoion gpsInformatoion = new GPSInformatoion(new Models.GPS.Position(reading.Position.Latitude, reading.Position.Longitude),
                                                   reading.PositionAccuracy,
                                                   reading.Timestamp,
                                                   reading.Heading,
                                                   reading.HeadingAccuracy,
                                                   reading.Altitude,
                                                   reading.Speed,
                                                   reading.SpeedAccuracy);

                        GpsDataStorageService?.StoreData(gpsInformatoion);
                    });
            }
            catch(Exception ex)
            {
            }
        }

        public void StopListening()
        {
            GpsManager?.StopListener();
        }
    }
}
