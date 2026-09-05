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

    /// <summary>Id of the trip type assigned to the next trip that gets created.</summary>
    Guid? CurrentTripTypeId { get; set; }

    /// <summary>Ends the current recording session so the next one creates a fresh trip.</summary>
    void FinalizeTrip();
}
