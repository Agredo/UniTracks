using UniTracks.Services.Stats;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Stats;

/// <summary>
/// The best efforts quote the fastest stretch of a given length inside one trip. They must ignore
/// standstill, never average across a GPS gap, and stay silent when the trip is too short.
/// </summary>
public class BestEffortCalculatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 4, 4, 9, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void ConstantPace_ReturnsTheExactTimeForTheDistance()
    {
        // 3 km at 4 m/s: 1 km takes exactly 250 s, 5 km does not fit.
        var track = Straight(distanceMeters: 3000, speed: 4.0);

        var efforts = BestEffortCalculator.Compute(track);

        var one = Assert.Single(efforts.Where(effort => effort.DistanceMeters == 1000));
        Assert.Equal(250, one.Seconds, 1);
        Assert.Equal(250, one.PaceSecondsPerKilometer, 1);
        Assert.DoesNotContain(efforts, effort => effort.DistanceMeters >= 5000);
    }

    [Fact]
    public void LongTrip_ReportsEveryDistanceItCovers()
    {
        // 12 km at 3 m/s, so 1 km, 5 km and 10 km are all inside the trip.
        var track = Straight(distanceMeters: 12000, speed: 3.0);

        var efforts = BestEffortCalculator.Compute(track);

        Assert.Equal(new[] { 1000.0, 5000.0, 10000.0 }, efforts.Select(effort => effort.DistanceMeters));
        Assert.All(efforts, effort => Assert.Equal(333.3, effort.PaceSecondsPerKilometer, 1));
    }

    [Fact]
    public void SlowerStart_IsSkippedInFavourOfTheFastSection()
    {
        var track = new List<LocationModel>();
        track.AddRange(Straight(distanceMeters: 1000, speed: 2.0, startAtMeters: 0));
        track.AddRange(Straight(distanceMeters: 1000, speed: 5.0, startAtMeters: 1000, timeOffsetSeconds: 500));

        var effort = BestEffortCalculator.Compute(track, new[] { 1000.0 }).Single();

        // The fast kilometre needs 200 s; the slow one would be 500 s.
        Assert.Equal(200, effort.Seconds, 1);
    }

    [Fact]
    public void StandstillInsideTheStretch_IsNotChargedToTheTime()
    {
        var track = Straight(distanceMeters: 1200, speed: 4.0);
        // A one-minute stop halfway: same position, zero speed, timestamp advanced.
        var stopAt = track[60];
        track.Insert(61, new LocationModel
        {
            ID = Guid.NewGuid(),
            TripID = stopAt.TripID,
            Latitude = stopAt.Latitude,
            Longitude = stopAt.Longitude,
            Altitude = stopAt.Altitude,
            Accuracy = stopAt.Accuracy,
            Speed = 0,
            Timestamp = stopAt.Timestamp.AddSeconds(60),
        });
        for (int index = 62; index < track.Count; index++)
        {
            track[index].Timestamp = track[index].Timestamp.AddSeconds(60);
        }

        var effort = BestEffortCalculator.Compute(track, new[] { 1000.0 }).Single();

        Assert.Equal(250, effort.Seconds, 1);
    }

    [Fact]
    public void GpsGap_IsNeverBridged()
    {
        // 1.8 km, but a two-minute gap splits them into two 900 m pieces.
        var track = Straight(distanceMeters: 1800, speed: 4.0);
        for (int index = 91; index < track.Count; index++)
        {
            track[index].Timestamp = track[index].Timestamp.AddMinutes(2);
        }

        var efforts = BestEffortCalculator.Compute(track, new[] { 1000.0 });

        // No single piece reaches 1000 m without crossing the gap.
        Assert.Empty(efforts);
    }

    [Fact]
    public void TripShorterThanTheDistance_ProducesNoEffort()
    {
        var track = Straight(distanceMeters: 800, speed: 4.0);

        Assert.Empty(BestEffortCalculator.Compute(track));
    }

    [Fact]
    public void TrackWithOnePoint_ProducesNoEffort()
    {
        var track = Straight(distanceMeters: 5000, speed: 4.0);

        Assert.Empty(BestEffortCalculator.Compute(new[] { track[0] }));
    }

    /// <summary>A straight northward line at constant speed, optionally shifted in space and time.</summary>
    private static List<LocationModel> Straight(
        double distanceMeters,
        double speed,
        double startAtMeters = 0,
        double timeOffsetSeconds = 0)
    {
        const double degreesPerMeter = 1.0 / 111_195.0;
        const double spacing = 10;

        int steps = Math.Max(1, (int)Math.Round(distanceMeters / spacing));
        double step = distanceMeters / steps;

        var track = new List<LocationModel>(steps + 1);
        for (int index = 0; index <= steps; index++)
        {
            double alongMeters = startAtMeters + index * step;
            track.Add(new LocationModel
            {
                ID = Guid.NewGuid(),
                TripID = Guid.Empty,
                Latitude = 50.0 + alongMeters * degreesPerMeter,
                Longitude = 8.0,
                Altitude = 100,
                Accuracy = 5,
                Speed = speed,
                Timestamp = Start.AddSeconds(timeOffsetSeconds + index * step / speed),
            });
        }

        return track;
    }
}
