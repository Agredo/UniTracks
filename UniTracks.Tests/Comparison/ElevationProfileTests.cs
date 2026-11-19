using UniTracks.Services.Comparison;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Comparison;

/// <summary>
/// The elevation profile of a comparison has to run over the distance every trip covered, otherwise
/// the curves would describe different stretches and lie about which climb belongs where.
/// </summary>
public class ElevationProfileTests
{
    [Fact]
    public void TwoTrips_ShareOneAxisAndOneSupportPointCount()
    {
        var a = Side("A", 2000, 100, 130);
        var b = Side("B", 2000, 100, 110);

        var set = TripComparisonCalculator.CompareSet(new[] { a, b });

        Assert.True(set.HasElevationProfile);
        Assert.Equal(2, set.ElevationSeries.Count);
        Assert.All(set.ElevationSeries, series => Assert.Equal(set.ElevationAxisLabels.Count, series.Count));

        // Both profiles start at the same altitude and end at their own summit.
        Assert.Equal(100, set.ElevationSeries[0][0], 1);
        Assert.Equal(130, set.ElevationSeries[0][^1], 1);
        Assert.Equal(110, set.ElevationSeries[1][^1], 1);

        Assert.Equal("0,0", set.ElevationAxisLabels[0]);
        Assert.Equal("2,0", set.ElevationAxisLabels[^1]);
    }

    [Fact]
    public void ShorterTrip_StopsTheSharedAxisAtItsOwnEnd()
    {
        var a = Side("Lang", 3000, 100, 150);
        var b = Side("Kurz", 1500, 100, 120);

        var set = TripComparisonCalculator.CompareSet(new[] { a, b });

        Assert.True(set.HasElevationProfile);

        // The shared axis ends where the shorter trip ends: 1.5 km.
        Assert.Equal("1,5", set.ElevationAxisLabels[^1]);

        // The longer profile is cut at the same distance, not compressed onto its own range.
        Assert.Equal(125, set.ElevationSeries[0][^1], 1);
    }

    [Fact]
    public void ClimbingProfilesAreNotFlat_SoTheChartHasSomethingToDraw()
    {
        var a = Side("A", 2000, 100, 130);
        var b = Side("B", 2000, 100, 110);

        var set = TripComparisonCalculator.CompareSet(new[] { a, b });

        Assert.All(set.ElevationSeries, series => Assert.True(series.Max() - series.Min() > 1));
    }

    [Fact]
    public void TrackWithoutAltitudeChange_StillProducesAReadableAxis()
    {
        var a = Side("A", 2000, 100, 100);
        var b = Side("B", 1800, 100, 100);

        var set = TripComparisonCalculator.CompareSet(new[] { a, b });

        Assert.True(set.HasElevationProfile);
        Assert.All(set.ElevationSeries, series => Assert.All(series, value => Assert.Equal(100, value, 1)));
    }

    [Fact]
    public void TooShortSharedDistance_SkipsTheProfile()
    {
        var a = Side("A", 50, 100, 130);
        var b = Side("B", 50, 100, 110);

        var set = TripComparisonCalculator.CompareSet(new[] { a, b });

        Assert.False(set.HasElevationProfile);
        Assert.Empty(set.ElevationSeries);
        Assert.Empty(set.ElevationAxisLabels);
    }

    /// <summary>A straight 10 m-spaced track that climbs evenly from one altitude to the other.</summary>
    private static TripComparisonSide Side(
        string name,
        double distanceMeters,
        double startAltitude,
        double endAltitude)
    {
        const double degreesPerMeter = 1.0 / 111_195.0;

        int steps = Math.Max(1, (int)Math.Round(distanceMeters / 10));
        double step = distanceMeters / steps;
        var id = Guid.NewGuid();
        var start = ComparisonFixtures.DefaultStart;

        var track = new List<LocationModel>(steps + 1);
        for (int index = 0; index <= steps; index++)
        {
            double fraction = index / (double)steps;
            track.Add(new LocationModel
            {
                ID = Guid.NewGuid(),
                TripID = id,
                Latitude = 50.0 + index * step * degreesPerMeter,
                Longitude = 8.0,
                Altitude = startAltitude + (endAltitude - startAltitude) * fraction,
                Accuracy = 5,
                Speed = 4.0,
                Timestamp = start.AddSeconds(index * step / 4.0),
            });
        }

        var trip = ComparisonFixtures.Trip(id, name, track);
        return TripComparisonCalculator.CreateSide(trip, track);
    }
}
