using UniTracks.Models.Trip;
using UniTracks.Tests.Comparison;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="TripEditPopupViewModel"/>. The popup edits metadata only — name, note and
/// type — and reports the outcome through the <c>Completed</c> event: a result on save, null on
/// cancel.
/// </summary>
public sealed class TripEditPopupViewModelTests
{
    private static TripEditPopupViewModel Create(FakePopupNavigationService popups, params TripType[] types) =>
        new(popups, new FakeTripTypeCatalog(types));

    private static TripEditResult? Capture(TripEditPopupViewModel viewModel, Action action)
    {
        TripEditResult? result = null;
        var raised = false;
        viewModel.Completed += (_, value) =>
        {
            raised = true;
            result = value;
        };

        action();

        Assert.True(raised, "Expected the popup to complete.");
        return result;
    }

    [Fact]
    public async Task Initialize_PrefillsNameNoteAndType()
    {
        var run = ComparisonFixtures.Type("run", "running");
        var viewModel = Create(new FakePopupNavigationService(), run);

        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            Name = "Runde am See",
            Description = "mit Hund",
            TripTypeId = run.ID,
            TripType = run,
            Locations = new(),
        };

        await viewModel.InitializeAsync(trip);

        Assert.Equal("Runde am See", viewModel.Name);
        Assert.Equal("mit Hund", viewModel.Description);
        Assert.Same(run, viewModel.SelectedType);
        Assert.Equal("run", viewModel.SelectedTypeName);
    }

    [Fact]
    public async Task Initialize_ResolvesTheTypeThroughTheCatalogueWhenTheNavigationIsMissing()
    {
        // LiteDB stores only the type id on the trip, so the navigation is null there.
        var run = ComparisonFixtures.Type("run", "running");
        var viewModel = Create(new FakePopupNavigationService(), run);

        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            TripTypeId = run.ID,
            TripType = null,
            Locations = new(),
        };

        await viewModel.InitializeAsync(trip);

        Assert.Same(run, viewModel.SelectedType);
    }

    [Fact]
    public async Task Initialize_WithoutType_ShowsTheFallbackLabel()
    {
        var viewModel = Create(new FakePopupNavigationService());

        await viewModel.InitializeAsync(new Trip { ID = Guid.NewGuid(), Locations = new() });

        Assert.Null(viewModel.SelectedType);
        Assert.Equal("Kein Typ", viewModel.SelectedTypeName);
    }

    [Fact]
    public async Task Save_TrimsTheFieldsAndTurnsWhitespaceIntoNull()
    {
        var run = ComparisonFixtures.Type("run", "running");
        var viewModel = Create(new FakePopupNavigationService(), run);
        await viewModel.InitializeAsync(new Trip { ID = Guid.NewGuid(), Locations = new() });

        viewModel.Name = "  Runde am See  ";
        viewModel.Description = "   ";
        viewModel.SelectedType = run;

        var result = Capture(viewModel, () => viewModel.SaveCommand.Execute(null));

        Assert.NotNull(result);
        Assert.Equal("Runde am See", result!.Name);
        Assert.Null(result.Description);
        Assert.Same(run, result.TripType);
    }

    [Fact]
    public async Task Cancel_ReportsNull()
    {
        var viewModel = Create(new FakePopupNavigationService());
        await viewModel.InitializeAsync(new Trip { ID = Guid.NewGuid(), Locations = new() });

        var result = Capture(viewModel, () => viewModel.CancelCommand.Execute(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task ChangeType_WhenTheSearchIsDismissed_KeepsTheCurrentType()
    {
        // The popup fake answers every popup with default — for the type search that is "dismissed".
        var run = ComparisonFixtures.Type("run", "running");
        var popups = new FakePopupNavigationService();
        var viewModel = Create(popups, run);
        await viewModel.InitializeAsync(new Trip
        {
            ID = Guid.NewGuid(),
            TripTypeId = run.ID,
            TripType = run,
            Locations = new(),
        });

        await viewModel.ChangeTypeCommand.ExecuteAsync(null);

        Assert.Same(run, viewModel.SelectedType);
        Assert.Contains(nameof(TripTypeSearchPopupViewModel), popups.ShownPopups);
    }
}
