using System.Diagnostics;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using UniTracks.Services.Location;

namespace UniTracks.Services.Data;

/// <summary>
/// Recalculates the stored distance of already-recorded trips with the <see cref="TrackSmoother"/>.
/// Trips recorded before smoothing existed carry the raw incremental distance, which overestimates
/// due to GPS jitter. Runs once per app start; trips whose smoothed distance matches the stored
/// value are skipped, so the pass is cheap after the first run.
/// </summary>
public class TripDistanceRecalculator
{
    private readonly IRepository _repository;
    private bool _ran;

    public TripDistanceRecalculator(IRepository repository)
    {
        _repository = repository;
    }

    public async Task RecalculateAsync()
    {
        if (_ran)
        {
            return;
        }
        _ran = true;

        try
        {
            var trips = (await _repository.GetAllAsync<Trip>(t => t.Locations)).ToList();

            foreach (var trip in trips)
            {
                if (trip.Locations is null || trip.Locations.Count < 3)
                {
                    continue;
                }

                double smoothed = TrackSmoother.SmoothedDistanceMeters(trip.Locations);

                // Only write when the value actually changed (0.1 m tolerance), so repeat
                // runs on already-recalculated trips touch nothing.
                if (trip.Distance is null || Math.Abs(trip.Distance.Value - smoothed) > 0.1)
                {
                    trip.Distance = smoothed;
                    await _repository.Update<Trip>(trip);
                }
            }
        }
        catch (Exception ex)
        {
            // Best-effort: a failure must never prevent app startup.
            Debug.WriteLine($"TripDistanceRecalculator failed: {ex}");
        }
    }
}
