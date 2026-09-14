using UniTracks.Models.Trip;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for the filter sheet. The value of the sheet is that the user sees the result count before
/// closing it, so most of these tests check the counter rather than the internal selection.
/// </summary>
public sealed class TripFilterPopupViewModelTests
{
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
        TripType = typeId is null
            ? null
            : new TripType { ID = typeId.Value, Name = typeId == Running ? "Laufen" : "Radfahren" },
        StartTime = startTime,
        EndTime = startTime + (duration ?? TimeSpan.FromMinutes(45)),
        Distance = distance,
        Locations = new(),
    };

    private static TripFilterChip ChipFor(TripFilterPopupViewModel viewModel, string label) =>
        viewModel.TypeChips.Concat(viewModel.DateChips)
            .Concat(viewModel.DurationChips)
            .Concat(viewModel.DistanceChips)
            .First(chip => chip.Label == label);

    private static async Task<TripFilterPopupViewModel> OpenedAsync(
        IReadOnlyList<Trip> trips,
        TripFilters? current = null)
    {
        var viewModel = new TripFilterPopupViewModel();
        await viewModel.InitializeAsync(trips, current ?? TripFilters.Empty);
        return viewModel;
    }

    [Fact]
    public async Task TypeChips_AreBuiltFromTheTripsThatExist_SortedByCountThenName()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("Lauf 1", Running, now),
            NewTrip("Lauf 2", Running, now.AddMinutes(-1)),
            NewTrip("Rad 1", Cycling, now.AddMinutes(-2)),
        });

        // Only the types the user actually recorded, most used first - not the whole catalogue.
        Assert.Collection(
            viewModel.TypeChips,
            chip => Assert.Equal("Laufen", chip.Label),
            chip => Assert.Equal("Radfahren", chip.Label));

        Assert.Equal(2, viewModel.TypeChips[0].Count);
        Assert.Equal(1, viewModel.TypeChips[1].Count);
        Assert.True(viewModel.HasTypeChips);
    }

    [Fact]
    public async Task TwoTypes_AreOrEd_AndTheCounterShowsBoth()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("Lauf", Running, now),
            NewTrip("Rad", Cycling, now.AddMinutes(-1)),
            NewTrip("Ohne Typ", null, now.AddMinutes(-2)),
        });

        ChipFor(viewModel, "Laufen").ToggleCommand.Execute(null);
        Assert.Equal(1, viewModel.ResultCount);

        ChipFor(viewModel, "Radfahren").ToggleCommand.Execute(null);

        Assert.Equal(2, viewModel.ResultCount);
        Assert.True(ChipFor(viewModel, "Laufen").IsSelected);
        Assert.True(ChipFor(viewModel, "Radfahren").IsSelected);
    }

    [Fact]
    public async Task DateRange_IsASingleChoice()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("heute", Running, now.AddHours(-1)),
            NewTrip("vor 40 Tagen", Running, now.AddDays(-40)),
        });

        ChipFor(viewModel, "Letzte 7 Tage").ToggleCommand.Execute(null);
        Assert.Equal(1, viewModel.ResultCount);

        ChipFor(viewModel, "Letzte 30 Tage").ToggleCommand.Execute(null);

        Assert.False(ChipFor(viewModel, "Letzte 7 Tage").IsSelected);
        Assert.True(ChipFor(viewModel, "Letzte 30 Tage").IsSelected);
        Assert.Equal(1, viewModel.ResultCount);

        // "Gesamt" is the way back to "no date filter" without leaving the group.
        ChipFor(viewModel, "Gesamt").ToggleCommand.Execute(null);
        Assert.Equal(2, viewModel.ResultCount);
    }

    [Fact]
    public async Task Duration_UsesEndMinusStart()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("kurz", Running, now, duration: TimeSpan.FromMinutes(20)),
            NewTrip("mittel", Running, now.AddMinutes(-1), duration: TimeSpan.FromMinutes(45)),
            NewTrip("lang", Running, now.AddMinutes(-2), duration: TimeSpan.FromMinutes(90)),
        });

        ChipFor(viewModel, "über 60 min").ToggleCommand.Execute(null);

        Assert.Equal(1, viewModel.ResultCount);

        ChipFor(viewModel, "unter 30 min").ToggleCommand.Execute(null);

        // Several durations are combined, so both ends survive.
        Assert.Equal(2, viewModel.ResultCount);
    }

    [Fact]
    public async Task Distance_IsBucketisedInMetres()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("drei km", Running, now, distance: 3000),
            NewTrip("acht km", Running, now.AddMinutes(-1), distance: 8000),
            NewTrip("fünfzehn km", Running, now.AddMinutes(-2), distance: 15000),
            NewTrip("ohne Distanz", Running, now.AddMinutes(-3)),
        });

        ChipFor(viewModel, "unter 5 km").ToggleCommand.Execute(null);
        Assert.Equal(2, viewModel.ResultCount);

        ChipFor(viewModel, "Halbmarathon+").ToggleCommand.Execute(null);
        Assert.Equal(2, viewModel.ResultCount);
    }

    [Fact]
    public async Task TypeAndDuration_AreAndEd()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("kurzer Lauf", Running, now, duration: TimeSpan.FromMinutes(20)),
            NewTrip("langer Lauf", Running, now.AddMinutes(-1), duration: TimeSpan.FromMinutes(90)),
            NewTrip("langes Rad", Cycling, now.AddMinutes(-2), duration: TimeSpan.FromMinutes(90)),
        });

        ChipFor(viewModel, "Laufen").ToggleCommand.Execute(null);
        ChipFor(viewModel, "über 60 min").ToggleCommand.Execute(null);

        Assert.Equal(1, viewModel.ResultCount);
        Assert.True(viewModel.CanApply);
    }

    [Fact]
    public async Task WithoutMatches_TheCounterIsZeroAndApplyIsDisabled()
    {
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("Lauf", Running, DateTimeOffset.Now, distance: 3000),
        });

        Assert.True(viewModel.CanApply);
        Assert.Equal(1, viewModel.ResultCount);

        ChipFor(viewModel, "Halbmarathon+").ToggleCommand.Execute(null);

        Assert.Equal(0, viewModel.ResultCount);
        Assert.False(viewModel.CanApply);
    }

    [Fact]
    public async Task Reset_DropsTheWholeSelection()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("Lauf", Running, now),
            NewTrip("Rad", Cycling, now.AddMinutes(-1)),
        });

        ChipFor(viewModel, "Laufen").ToggleCommand.Execute(null);
        ChipFor(viewModel, "Letzte 7 Tage").ToggleCommand.Execute(null);
        Assert.True(viewModel.HasSelection);

        viewModel.ResetCommand.Execute(null);

        Assert.False(viewModel.HasSelection);
        Assert.False(ChipFor(viewModel, "Laufen").IsSelected);
        Assert.False(ChipFor(viewModel, "Letzte 7 Tage").IsSelected);
        Assert.True(ChipFor(viewModel, "Gesamt").IsSelected);
        Assert.Equal(2, viewModel.ResultCount);
    }

    [Fact]
    public async Task Apply_ReturnsTheSelection_CancelReturnsNothing()
    {
        var now = DateTimeOffset.Now;
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("Lauf", Running, now),
            NewTrip("Rad", Cycling, now.AddMinutes(-1)),
        });

        ChipFor(viewModel, "Laufen").ToggleCommand.Execute(null);

        TripFilters? applied = null;
        viewModel.Completed += (_, result) => applied = result;
        viewModel.ApplyCommand.Execute(null);

        Assert.NotNull(applied);
        Assert.Contains(Running, applied!.TypeIds);
        Assert.Equal(1, applied.ActiveCount);

        TripFilters? cancelled = TripFilters.Empty;
        viewModel.Completed += (_, result) => cancelled = result;
        viewModel.CancelCommand.Execute(null);

        Assert.Null(cancelled);
    }

    [Fact]
    public async Task Reopening_TheSheet_ShowsTheFilterInUse()
    {
        var now = DateTimeOffset.Now;
        var current = new TripFilters
        {
            TypeIds = new[] { Cycling },
            DateRange = DateRangePreset.Last30Days,
        };

        var viewModel = await OpenedAsync(
            new[]
            {
                NewTrip("Lauf", Running, now),
                NewTrip("Rad", Cycling, now.AddMinutes(-1)),
            },
            current);

        Assert.True(ChipFor(viewModel, "Radfahren").IsSelected);
        Assert.False(ChipFor(viewModel, "Laufen").IsSelected);
        Assert.True(ChipFor(viewModel, "Letzte 30 Tage").IsSelected);
        Assert.Equal(1, viewModel.ResultCount);
    }

    [Fact]
    public async Task WithoutAnyTripType_TheTypeGroupIsHidden()
    {
        var viewModel = await OpenedAsync(new[]
        {
            NewTrip("Ohne Typ", null, DateTimeOffset.Now),
        });

        Assert.Empty(viewModel.TypeChips);
        Assert.False(viewModel.HasTypeChips);
        Assert.Equal(1, viewModel.ResultCount);
    }
}
