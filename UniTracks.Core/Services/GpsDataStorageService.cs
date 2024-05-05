using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Models.GPS;
using UniTracks.Services.Data;

namespace UniTracks.Core.Services;

public class GpsDataStorageService : IGpsDataStorageService
{
    public async Task StoreData(GPSInformatoion gpsInformatoion, Action<GPSInformatoion> action)
    {
        var latituede = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;
        Console.WriteLine($"Latitude: {latituede}, Longitude: {longitude}");
        action(gpsInformatoion);
    }

    public async Task StoreData(GPSInformatoion gpsInformatoion)
    {
        var latituede = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;
        Console.WriteLine($"Latitude: {latituede}, Longitude: {longitude}");
    }
}
