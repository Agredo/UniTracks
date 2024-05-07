using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Models.GPS;

namespace UniTracks.Services.Data;

public interface IGpsDataStorageService
{
    Task StoreData(GPSInformatoion gpsInformatoion, Action<GPSInformatoion> action);
    Task StoreData(GPSInformatoion gpsInformatoion);

    Task<List<Models.Location.Location>> getAll();
}
