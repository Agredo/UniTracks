using UniTracks.Data.Repository;
using UniTracks.Models.GPS;
using UniTracks.Models.Trip;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Services.Data;

public class GpsDataStorageService : IGpsDataStorageService
{
    private Trip? currentTrip;

    private LocationModel? previousLocation;
    private int locationCount;
    private double totalSpeed;
    private double totalAltitude;
    private double totalHeading;
    private double totalAccuracy;
    private double minSpeed;
    private double maxSpeed;
    private double minAltitude;
    private double maxAltitude;
    private double minHeading;
    private double maxHeading;
    private double minAccuracy;
    private double maxAccuracy;
    private double distance;

    // Serializes StoreData / FinalizeTrip so the singleton EF context and the in-flight
    // currentTrip are never mutated concurrently (EF Core's DbContext is not thread-safe).
    private readonly SemaphoreSlim storeGate = new(1, 1);

    public GpsDataStorageService(IRepository repository)
    {
        Repository = repository;
    }

    public IRepository Repository { get; }

    public Guid? CurrentTripTypeId { get; set; }

    public void FinalizeTrip()
    {
        storeGate.Wait();
        try
        {
            currentTrip = null;
            ResetAggregates();
        }
        finally
        {
            storeGate.Release();
        }
    }

    public async Task<List<LocationModel>> getAll()
    {
        return (await Repository.GetAllAsync<LocationModel>()).ToList();
    }

    public async Task StoreData(GPSInformatoion gpsInformatoion, Action<GPSInformatoion> action)
    {
        var latitude = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;
        Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");
        action(gpsInformatoion);
    }

    public async Task StoreData(GPSInformatoion gpsInformatoion)
    {
        var latitude = gpsInformatoion.Position.Latitude;
        var longitude = gpsInformatoion.Position.Longitude;

        LocationModel location = new LocationModel()
        {
            ID = Guid.NewGuid(),
            Latitude = latitude,
            Longitude = longitude,
            Altitude = gpsInformatoion.Altitude,
            Accuracy = gpsInformatoion.PositionAccuracy,
            Speed = gpsInformatoion.Speed,
            Heading = gpsInformatoion.Heading,
            HeadingAccuracy = gpsInformatoion.HeadingAccuracy,
            SpeedAccuracy = gpsInformatoion.SpeedAccuracy,
            Timestamp = gpsInformatoion.Timestamp
        };

        await storeGate.WaitAsync();
        try
        {
            if (currentTrip != null)
            {
                // Link the location to the tracked trip and update aggregates incrementally.
                location.TripID = currentTrip.ID;
                currentTrip.Locations.Add(location);
                AccountFor(location);
                ApplyAggregates(currentTrip);

                // Persist the new location as a brand-new row, then write the trip's stats.
                // Update<Trip> only touches the trip's scalar properties now (no graph cascade).
                await Repository.Add<LocationModel>(location);
                await Repository.Update<Trip>(currentTrip);
            }
            else
            {
                ResetAggregates();
                AccountFor(location);

                var trip = new Trip()
                {
                    ID = Guid.NewGuid(),
                    StartTime = DateTimeOffset.Now,
                    TripTypeId = CurrentTripTypeId,
                    Locations = new List<LocationModel>() { location }
                };

                location.TripID = trip.ID;
                ApplyAggregates(trip);

                currentTrip = await Repository.Add<Trip>(trip);
            }
        }
        finally
        {
            storeGate.Release();
        }

        Console.WriteLine($"CurrentTrip: {currentTrip.ID} {currentTrip.StartTime} Latitude: {currentTrip.Locations.Last().Latitude}, Longitude: {currentTrip.Locations.Last().Longitude}");
    }

    private void AccountFor(LocationModel location)
    {
        locationCount++;
        totalSpeed += location.Speed;
        totalAltitude += location.Altitude;
        totalHeading += location.Heading;
        totalAccuracy += location.Accuracy;

        if (locationCount == 1)
        {
            minSpeed = maxSpeed = location.Speed;
            minAltitude = maxAltitude = location.Altitude;
            minHeading = maxHeading = location.Heading;
            minAccuracy = maxAccuracy = location.Accuracy;
        }
        else
        {
            minSpeed = Math.Min(minSpeed, location.Speed);
            maxSpeed = Math.Max(maxSpeed, location.Speed);
            minAltitude = Math.Min(minAltitude, location.Altitude);
            maxAltitude = Math.Max(maxAltitude, location.Altitude);
            minHeading = Math.Min(minHeading, location.Heading);
            maxHeading = Math.Max(maxHeading, location.Heading);
            minAccuracy = Math.Min(minAccuracy, location.Accuracy);
            maxAccuracy = Math.Max(maxAccuracy, location.Accuracy);
        }

        if (previousLocation != null)
        {
            distance += HaversineDistance(previousLocation, location);
        }

        previousLocation = location;
    }

    private void ApplyAggregates(Trip trip)
    {
        trip.MaxSpeed = maxSpeed;
        trip.MinSpeed = minSpeed;
        trip.MaxAltitude = maxAltitude;
        trip.MinAltitude = minAltitude;
        trip.MaxHeading = maxHeading;
        trip.MinHeading = minHeading;
        trip.MaxAccuracy = maxAccuracy;
        trip.MinAccuracy = minAccuracy;
        trip.AverageSpeed = locationCount > 0 ? totalSpeed / locationCount : 0;
        trip.Distance = distance;
    }

    private void ResetAggregates()
    {
        previousLocation = null;
        locationCount = 0;
        totalSpeed = totalAltitude = totalHeading = totalAccuracy = 0;
        minSpeed = maxSpeed = minAltitude = maxAltitude = minHeading = maxHeading = minAccuracy = maxAccuracy = 0;
        distance = 0;
    }

    /// <summary>Distance between two coordinates in meters, using the Haversine formula.</summary>
    private static double HaversineDistance(LocationModel location1, LocationModel location2)
    {
        const double earthRadius = 6371000;

        double dLat = ToRadians(location2.Latitude - location1.Latitude);
        double dLon = ToRadians(location2.Longitude - location1.Longitude);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(location1.Latitude)) * Math.Cos(ToRadians(location2.Latitude)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadius * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
