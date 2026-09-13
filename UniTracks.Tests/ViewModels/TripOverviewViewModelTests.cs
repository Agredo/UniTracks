using UniTracks.Models.Trip;
using UniTracks.Tests.Comparison;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.Pages;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for the edit flow of <see cref="TripOverviewViewModel"/>: the popup reports a
/// <see cref="TripEditResult"/>, the ViewModel persists it and only rebuilds the fingerprint when
/// the type actually changed.
/// </summary>
public sealed class TripOverviewViewModelTests
{
    private sealed class Fixture
    {
        public Fixture(Trip trip)
        {
            Navigation.Parameters["parameter"] = trip;
            Repository.Seed(trip);

            ViewModel = new TripOverviewViewModel(
                Navigation,
                Popups,
                Repository,
                Fingerprints,
                new FakeTrackSmoothingSettings(),
                new FakeMapStyleSettings());
        }

        public FakeNavigationService Navigation { get; } = new();

        public FakePopupNavigationService Popups { get; } = new();

        public InMemoryRepository Repository { get; } = new();

        public FakeTripFingerprintService Fingerprints { get; } = new();

        public TripOverviewViewModel ViewModel { get; }
    }

    private static Trip NewTrip(string? name = null, TripType? type = null)
    {
        var start = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.FromHours(1));
        return new Trip
        {
            ID = Guid.NewGuid(),
            Name = name,
            StartTime = start,
            EndTime = start.AddHours(1),
            TripTypeId = type?.ID,
            TripType = type,
            Locations = new(),
        };
    }

    [Fact]
    public async Task ApplyEdit_PersistsNameNoteAndType()
    {
        var run = ComparisonFixtures.Type("run", "running");
        var walk = ComparisonFixtures.Type("walk", "running");
        var fixture = new Fixture(NewTrip(type: run));

        await fixture.ViewModel.ApplyEditAsync(new TripEditResult("Runde am See", "mit Hund", walk));

        var trip = fixture.ViewModel.Trip!;
        Assert.Equal("Runde am See", trip.Name);
        Assert.Equal("mit Hund", trip.Description);
        Assert.Equal(walk.ID, trip.TripTypeId);
        Assert.Equal("Runde am See", fixture.ViewModel.TripName);

        var update = Assert.Single(fixture.Repository.Calls, call => call.Operation == "Update" && call.EntityType == typeof(Trip));
        Assert.Same(trip, Assert.IsType<Trip>(Assert.Single(update.Entities)));
    }

    [Fact]
    public async Task ApplyEdit_WhenTheTypeChanged_RebuildsTheFingerprint()
    {
        var run = ComparisonFixtures.Type("run", "running");
        var walk = ComparisonFixtures.Type("walk", "running");
        var fixture = new Fixture(NewTrip(type: run));

        await fixture.ViewModel.ApplyEditAsync(new TripEditResult(null, null, walk));

        Assert.Equal(fixture.ViewModel.Trip!.ID, Assert.Single(fixture.Fingerprints.Rebuilt));
    }

    [Fact]
    public async Task ApplyEdit_WhenOnlyTheNameChanged_KeepsTheFingerprintUntouched()
    {
        var run = ComparisonFixtures.Type("run", "running");
        var fixture = new Fixture(NewTrip(type: run));

        await fixture.ViewModel.ApplyEditAsync(new TripEditResult("Neuer Name", null, run));

        Assert.Empty(fixture.Fingerprints.Rebuilt);
        Assert.Equal(run.ID, fixture.ViewModel.Trip!.TripTypeId);
    }

    [Fact]
    public async Task ApplyEdit_AnEmptyNameFallsBackToTheAutomaticName()
    {
        var fixture = new Fixture(NewTrip(name: "Runde am See"));

        await fixture.ViewModel.ApplyEditAsync(new TripEditResult(null, null, null));

        Assert.Null(fixture.ViewModel.Trip!.Name);
        Assert.Equal(fixture.ViewModel.TripName, UniTracks.Services.Comparison.TripDisplay.TimeOfDayName(fixture.ViewModel.Trip.StartTime));
    }

    [Fact]
    public void Constructor_PrefersACustomNameOverTheAutomaticOne()
    {
        var fixture = new Fixture(NewTrip(name: "Runde am See"));

        Assert.Equal("Runde am See", fixture.ViewModel.TripName);
    }
}
