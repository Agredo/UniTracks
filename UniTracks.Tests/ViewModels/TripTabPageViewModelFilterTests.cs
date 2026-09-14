using UniTracks.Models.Trip;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for the filter half of <see cref="TripTabPageViewModel"/>. The two things that break most
/// easily are paging over the unfiltered list and losing the filter on the next read, so both get a
/// test of their own.
/// </summary>
public sealed class TripTabPageViewModelFilterTests
{
    private sealed class Fixture
    {
        public Fixture(Action<InMemoryRepository>? seed = null, Action<FakeTripFilterSettings>? seedFilters = null)
        {
            // The constructor loads the trips, so the seed has to be in place beforehand.
            seed?.Invoke(Repository);
            seedFilters?.Invoke(FilterSettings);

            ViewModel = new TripTabPageViewModel(
                Navigation,
                Popups,
                Location,
                FileSystem,
                Gps,
                Repository,
                Fingerprints,
                CardLayout,
                FilterSettings);
        }

        public FakeNavigationService Navigation { get; } = new();

        public FakePopupNavigationService Popups { get; } = new();

        public FakeLocationService Location { get; } = new();

        public FakeFileSystem FileSystem { get; } = new();

        public FakeGpsDataStorageService Gps { get; } = new();

        public InMemoryRepository Repository { get; } = new();

        public FakeTripFingerprintService Fingerprints { get; } = new();

        public FakeTripCardLayoutSettings CardLayout { get; } = new();

        public FakeTripFilterSettings FilterSettings { get; } = new();

        public TripTabPageViewModel ViewModel { get; }
    }

    private static readonly Guid Running = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid Cycling = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Trip NewTrip(
        string name,
        Guid? typeId,
        DateTimeOffset startTime,
        TimeSpan? duration = null,
        double? distance = null) => new()
    {
        ID = Guid.NewGuid(),
        Name = name,
        TripTypeId = typeId,
        TripType = typeId is null ? null : new TripType { ID = typeId.Value, Name = name + "-typ" },
        StartTime = startTime,
        EndTime = startTime + (duration ?? TimeSpan.FromMinutes(45)),
        Distance = distance,
        Locations = new(),
    };

    [Fact]
    public void ApplyFilters_KeepsOnlyTheSelectedType()
    {
        var fixture = new Fixture(seed: repository =>
        {
            repository.Seed(
                NewTrip("Lauf", Running, DateTimeOffset.Now.AddHours(-2)),
                NewTrip("Rad", Cycling, DateTimeOffset.Now.AddHours(-1)));
        });

        fixture.ViewModel.ApplyFilters(new TripFilters { TypeIds = new[] { Running } });

        Assert.Single(fixture.ViewModel.Trips);
        Assert.Equal("Lauf", fixture.ViewModel.Trips[0].Name);
        Assert.True(fixture.ViewModel.HasActiveFilters);
        Assert.Equal(1, fixture.ViewModel.ActiveFilterCount);
        Assert.Equal(Running.ToString(), fixture.FilterSettings.TypeIds);
    }

    [Fact]
    public void TwoTypes_AreOrEd()
    {
        var fixture = new Fixture(seed: repository =>
        {
            repository.Seed(
                NewTrip("Lauf", Running, DateTimeOffset.Now.AddHours(-3)),
                NewTrip("Rad", Cycling, DateTimeOffset.Now.AddHours(-2)),
                NewTrip("Ohne Typ", null, DateTimeOffset.Now.AddHours(-1)));
        });

        fixture.ViewModel.ApplyFilters(new TripFilters { TypeIds = new[] { Running, Cycling } });

        Assert.Equal(2, fixture.ViewModel.Trips.Count);
        Assert.DoesNotContain(fixture.ViewModel.Trips, trip => trip.Name == "Ohne Typ");
    }

    [Fact]
    public void TypeAndDateRange_AreAndEd()
    {
        var fixture = new Fixture(seed: repository =>
        {
            repository.Seed(
                NewTrip("Lauf aktuell", Running, DateTimeOffset.Now.AddDays(-2)),
                NewTrip("Lauf alt", Running, DateTimeOffset.Now.AddDays(-40)),
                NewTrip("Rad aktuell", Cycling, DateTimeOffset.Now.AddDays(-2)));
        });

        fixture.ViewModel.ApplyFilters(new TripFilters
        {
            TypeIds = new[] { Running },
            DateRange = DateRangePreset.Last7Days,
        });

        Assert.Single(fixture.ViewModel.Trips);
        Assert.Equal("Lauf aktuell", fixture.ViewModel.Trips[0].Name);
        Assert.Equal(2, fixture.ViewModel.ActiveFilterCount);
    }

    [Fact]
    public void DurationComesFromEndMinusStart_NotFromTheNeverWrittenTotals()
    {
        var fixture = new Fixture(seed: repository =>
        {
            repository.Seed(
                NewTrip("kurz", Running, DateTimeOffset.Now.AddHours(-2), duration: TimeSpan.FromMinutes(20)),
                NewTrip("lang", Running, DateTimeOffset.Now.AddHours(-1), duration: TimeSpan.FromMinutes(90)));
        });

        fixture.ViewModel.ApplyFilters(new TripFilters { Durations = new[] { DurationBucket.Under30Minutes } });

        Assert.Single(fixture.ViewModel.Trips);
        Assert.Equal("kurz", fixture.ViewModel.Trips[0].Name);
    }

    [Fact]
    public void CombinationWithoutMatches_LeavesAnEmptyListAndKeepsTheFilterVisible()
    {
        var fixture = new Fixture(seed: repository =>
        {
            repository.Seed(NewTrip("Lauf", Running, DateTimeOffset.Now.AddDays(-40)));
        });

        fixture.ViewModel.ApplyFilters(new TripFilters { DateRange = DateRangePreset.Last7Days });

        Assert.Empty(fixture.ViewModel.Trips);
        Assert.True(fixture.ViewModel.HasActiveFilters);
        Assert.False(fixture.ViewModel.HasMoreTrips);
    }

    [Fact]
    public void ResetFilters_ShowsEveryTripAgain()
    {
        var fixture = new Fixture(seed: repository =>
        {
            repository.Seed(
                NewTrip("Lauf", Running, DateTimeOffset.Now.AddHours(-2)),
                NewTrip("Rad", Cycling, DateTimeOffset.Now.AddHours(-1)));
        });

        fixture.ViewModel.ApplyFilters(new TripFilters { TypeIds = new[] { Running } });
        fixture.ViewModel.ResetFiltersCommand.Execute(null);

        Assert.Equal(2, fixture.ViewModel.Trips.Count);
        Assert.Equal(0, fixture.ViewModel.ActiveFilterCount);
        Assert.False(fixture.ViewModel.HasActiveFilters);
        Assert.Equal(string.Empty, fixture.FilterSettings.TypeIds);
    }

    [Fact]
    public void LoadMore_PagesOverTheFilteredTripsOnly()
    {
        var start = DateTimeOffset.Now;
        var fixture = new Fixture(seed: repository =>
        {
            // The two types alternate, so paging over the unfiltered list would pull in a cycling
            // trip right after the first block.
            var trips = new List<Trip>();
            for (var i = 0; i < 40; i++)
            {
                trips.Add(NewTrip(
                    i % 2 == 0 ? "Lauf " + i : "Rad " + i,
                    i % 2 == 0 ? Running : Cycling,
                    start.AddMinutes(-i)));
            }

            repository.Seed(trips.ToArray());
        });

        fixture.ViewModel.ApplyFilters(new TripFilters { TypeIds = new[] { Running } });

        Assert.Equal(TripTabPageViewModel.PageSize, fixture.ViewModel.Trips.Count);
        Assert.True(fixture.ViewModel.HasMoreTrips);

        fixture.ViewModel.LoadMoreTripsCommand.Execute(null);

        Assert.Equal(20, fixture.ViewModel.Trips.Count);
        Assert.All(fixture.ViewModel.Trips, trip => Assert.Equal(Running, trip.TripTypeId));
        Assert.False(fixture.ViewModel.HasMoreTrips);
    }

    [Fact]
    public void Constructor_RestoresTheSavedFilter()
    {
        var fixture = new Fixture(
            seed: repository =>
            {
                repository.Seed(
                    NewTrip("Lauf", Running, DateTimeOffset.Now.AddHours(-2)),
                    NewTrip("Rad", Cycling, DateTimeOffset.Now.AddHours(-1)));
            },
            seedFilters: settings => settings.TypeIds = Cycling.ToString());

        Assert.Single(fixture.ViewModel.Trips);
        Assert.Equal("Rad", fixture.ViewModel.Trips[0].Name);
        Assert.Equal(1, fixture.ViewModel.ActiveFilterCount);

        // The constructor must not overwrite what it just read.
        Assert.Equal(Cycling.ToString(), fixture.FilterSettings.TypeIds);
    }

    [Fact]
    public void Refresh_KeepsTheFilterApplied()
    {
        var fixture = new Fixture(seed: repository =>
        {
            repository.Seed(
                NewTrip("Lauf", Running, DateTimeOffset.Now.AddHours(-2)),
                NewTrip("Rad", Cycling, DateTimeOffset.Now.AddHours(-1)));
        });

        fixture.ViewModel.ApplyFilters(new TripFilters { TypeIds = new[] { Running } });

        // Refresh re-reads from the store; the filter has to survive that read.
        fixture.ViewModel.RefreshCommand.Execute(null);

        Assert.Single(fixture.ViewModel.Trips);
        Assert.Equal(Running, fixture.ViewModel.Trips[0].TripTypeId);
        Assert.Equal(1, fixture.ViewModel.ActiveFilterCount);
    }
}
