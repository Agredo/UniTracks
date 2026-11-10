using System.Diagnostics;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;
using UniTracks.Services.Stats;

namespace UniTracks.Services.Data;

/// <summary>
/// Recalculates legacy recorded-trip metrics with the same raw-track rules used by the overview,
/// comparison and new recordings. Runs once per app start; matching trips are skipped, so the pass
/// is cheap after the first run.
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
                if (trip.Locations is null || trip.Locations.Count < 2)
                {
                    continue;
                }

                var ordered = trip.Locations.OrderBy(location => location.Timestamp).ToList();
                double distance = TripMetrics.DistanceMeters(trip, ordered);
                double elapsedSeconds = TripMetrics.ElapsedSeconds(trip);
                var (moving, stopped) = TripMetricsCalculator.ComputeMovingAndStopped(ordered);
                double movingSeconds = moving.TotalSeconds;
                // Long GPS gaps are unknown time, not a measured pause.
                double stoppedSeconds = stopped.TotalSeconds;
                double averageSpeed = movingSeconds > 0 ? distance / movingSeconds : 0;

                // Only write when a value actually changed, so repeat runs touch nothing.
                if (trip.Distance is null || Math.Abs(trip.Distance.Value - distance) > 0.1
                    || trip.AverageSpeed is null || Math.Abs(trip.AverageSpeed.Value - averageSpeed) > 0.001
                    || trip.TotalTime is null || Math.Abs(trip.TotalTime.Value - elapsedSeconds) > 0.001
                    || trip.MovingTime is null || Math.Abs(trip.MovingTime.Value - movingSeconds) > 0.001
                    || trip.StoppedTime is null || Math.Abs(trip.StoppedTime.Value - stoppedSeconds) > 0.001)
                {
                    trip.Distance = distance;
                    trip.TotalTime = elapsedSeconds;
                    trip.MovingTime = movingSeconds;
                    trip.StoppedTime = stoppedSeconds;
                    trip.AverageSpeed = averageSpeed;
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
