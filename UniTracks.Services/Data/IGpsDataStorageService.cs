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

    /// <summary>
    /// Id of the trip type assigned to the next trip that gets created. Prefer
    /// <see cref="ApplyTripTypeAsync"/>: it sets this and also writes the type to an open trip.
    /// </summary>
    Guid? CurrentTripTypeId { get; set; }

    /// <summary>
    /// Assigns the trip type to the trip that is being recorded right now - or, while no trip is open,
    /// to the next one that gets created. The trip row is written with the first GPS fix, but the
    /// activity can be changed for the whole recording, so the type has to reach the open trip as well.
    /// </summary>
    Task ApplyTripTypeAsync(Guid? tripTypeId);

    /// <summary>Ends the current recording session so the next one creates a fresh trip.</summary>
    void FinalizeTrip();

    /// <summary>True while a trip is being recorded and has not been finalised yet.</summary>
    bool IsTripInProgress { get; }
}
