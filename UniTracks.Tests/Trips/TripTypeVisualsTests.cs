using UniTracks.Models.Trip;
using UniTracks.Services.Trips;

namespace UniTracks.Tests.Trips;

/// <summary>
/// Guards the trip-type → icon/accent mapping behind the trip cards: every known category gets a
/// calm accent pair, identifiers can specialise the icon (paw for "Gassi gehen"), and unknown
/// input falls back to the neutral default instead of crashing the card.
/// </summary>
public sealed class TripTypeVisualsTests
{
    private static TripType Type(string? identifier, string? category) => new()
    {
        ID = Guid.NewGuid(),
        Name = identifier ?? "Unnamed",
        Identifier = identifier,
        Category = category,
    };

    [Fact]
    public void Null_FallsBackToTheDefaultVisual()
    {
        var visual = TripTypeVisuals.For(null);

        Assert.Equal("run.png", visual.Icon);
    }

    [Fact]
    public void UnknownCategory_FallsBackToTheDefaultVisual()
    {
        var visual = TripTypeVisuals.For(Type("spaceshuttle", "space sports"));

        Assert.Equal("run.png", visual.Icon);
    }

    [Theory]
    [InlineData("running", "run.png")]
    [InlineData("cycling", "bike.png")]
    [InlineData("winter sports", "ski.png")]
    [InlineData("skating", "skate.png")]
    [InlineData("water sports", "swim.png")]
    [InlineData("miscellaneous", "mountain.png")]
    [InlineData("fitness", "fitness.png")]
    [InlineData("fighting sports", "fight.png")]
    [InlineData("ball sports", "ball.png")]
    public void EveryKnownCategory_GetsItsOwnIcon(string category, string expectedIcon)
    {
        Assert.Equal(expectedIcon, TripTypeVisuals.For(Type(null, category)).Icon);
    }

    [Fact]
    public void CategoryMatching_IsCaseInsensitive()
    {
        Assert.Equal("bike.png", TripTypeVisuals.For(Type(null, "Cycling")).Icon);
    }

    [Fact]
    public void Dogwalk_GetsThePawIconButKeepsTheRunningAccent()
    {
        var dogwalk = TripTypeVisuals.For(Type("dogwalk", "running"));
        var run = TripTypeVisuals.For(Type("run", "running"));

        Assert.Equal("dog.png", dogwalk.Icon);
        Assert.Equal(run.Accent, dogwalk.Accent);
        Assert.Equal(run.Soft, dogwalk.Soft);
    }

    [Fact]
    public void UnknownIdentifier_KeepsTheCategoryIcon()
    {
        Assert.Equal("swim.png", TripTypeVisuals.For(Type("underwaterbasketweaving", "water sports")).Icon);
    }

    [Fact]
    public void NullIdentifierAndCategory_FallBackToTheDefaultVisual()
    {
        Assert.Equal("run.png", TripTypeVisuals.For(Type(null, null)).Icon);
    }
}
