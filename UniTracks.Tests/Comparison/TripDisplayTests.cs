using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;

namespace UniTracks.Tests.Comparison;

public class TripDisplayTests
{
    [Fact]
    public void RunningUsesPacePerKilometer()
    {
        var type = Type("run", "running");

        Assert.Equal(TripRateKind.PacePerKilometer, TripDisplay.RateKind(type));
        Assert.Equal("5:00", TripDisplay.FormatRate(type, 300));
        Assert.Equal("5:00 /km", TripDisplay.FormatRateWithUnit(type, 300));
    }

    [Fact]
    public void CyclingUsesKilometersPerHour()
    {
        var type = Type("cycling", "cycling");

        Assert.Equal(TripRateKind.SpeedKilometersPerHour, TripDisplay.RateKind(type));
        Assert.Equal("30,0", TripDisplay.FormatRate(type, 120));
        Assert.Equal("30,0 km/h", TripDisplay.FormatRateWithUnit(type, 120));
    }

    [Fact]
    public void SwimmingUsesPacePer100Meters()
    {
        var type = Type("openwaterswimming", "water sports");

        Assert.Equal(TripRateKind.PacePer100Meters, TripDisplay.RateKind(type));
        Assert.Equal("2:00", TripDisplay.FormatRate(type, 1200));
        Assert.Equal("2:00 /100 m", TripDisplay.FormatRateWithUnit(type, 1200));
    }

    private static TripType Type(string identifier, string category) => new()
    {
        ID = Guid.NewGuid(),
        Name = identifier,
        Identifier = identifier,
        Description = identifier,
        Category = category,
    };
}
