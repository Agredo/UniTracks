using UniTracks.Models.Trip;
using UniTracks.Services.Trips;

namespace UniTracks.Tests.Trips;

/// <summary>
/// The catalog is German, but the word a user types is often a different one. The search has to find
/// "Radfahren" for "Fahrrad" and "Laufen" for "Joggen", while identifier and name keep matching.
/// </summary>
public sealed class TripTypeSearchTermsTests
{
    private static TripType Type(string identifier, string name) => new()
    {
        ID = Guid.NewGuid(),
        Identifier = identifier,
        Name = name,
        Description = string.Empty,
        Category = "cycling",
    };

    [Theory]
    [InlineData("Radfahren")]
    [InlineData("rad")]
    [InlineData("Fahrrad")]
    [InlineData("fahrrad")]
    [InlineData("Rennrad")]
    [InlineData("radeln")]
    public void Matches_CyclingByNameAndGermanSynonyms(string term)
    {
        Assert.True(TripTypeSearchTerms.Matches(Type("cycling", "Radfahren"), term));
    }

    [Theory]
    [InlineData("Joggen")]
    [InlineData("jogging")]
    [InlineData("Laufen")]
    [InlineData("run")]
    public void Matches_RunningByNameSynonymAndIdentifier(string term)
    {
        Assert.True(TripTypeSearchTerms.Matches(Type("run", "Laufen"), term));
    }

    [Fact]
    public void Matches_FindsFootballWithoutTheSharpS()
    {
        var soccer = Type("soccer", "Fu\u00dfball");

        Assert.True(TripTypeSearchTerms.Matches(soccer, "Fu\u00dfball"));
        Assert.True(TripTypeSearchTerms.Matches(soccer, "fussball"));
        Assert.True(TripTypeSearchTerms.Matches(soccer, "FUSSBALL"));
    }

    [Fact]
    public void Matches_DoesNotInventMatchesForUnrelatedTerms()
    {
        Assert.False(TripTypeSearchTerms.Matches(Type("cycling", "Radfahren"), "schwimmen"));
        Assert.False(TripTypeSearchTerms.Matches(Type("run", "Laufen"), "golf"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Matches_BlankTermMatchesEverything(string? term)
    {
        Assert.True(TripTypeSearchTerms.Matches(Type("cycling", "Radfahren"), term));
    }

    [Fact]
    public void Matches_TrimsTheTerm()
    {
        Assert.True(TripTypeSearchTerms.Matches(Type("cycling", "Radfahren"), "  Radfahren  "));
    }
}
