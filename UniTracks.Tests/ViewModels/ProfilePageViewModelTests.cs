using UniTracks.Models.Health;
using UniTracks.Models.User;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Pages;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="ProfilePageViewModel"/>. The page is reachable straight from the profile tab,
/// so saving has to work on a device where the load has not finished yet: <see cref="ProfilePageViewModel.EnsureLoadedAsync"/>
/// is what prevents a second profile row from being created by a fast tap.
/// </summary>
public sealed class ProfilePageViewModelTests
{
    [Fact]
    public async Task Load_FillsTheFormFromTheStoredProfile()
    {
        var user = new User { ID = Guid.NewGuid(), Name = "Chris", Email = "chris@example.com", Height = 182 };
        var repository = new InMemoryRepository();
        repository.Seed(user);
        var viewModel = Create(repository);

        await viewModel.EnsureLoadedAsync();

        Assert.Equal("Chris", viewModel.Name);
        Assert.Equal("chris@example.com", viewModel.Email);
        Assert.Equal("182", viewModel.HeightText);
    }

    [Fact]
    public async Task Load_TakesTheNewestWeightEntry()
    {
        var user = new User { ID = Guid.NewGuid(), Name = "Chris", Height = 182 };
        var repository = new InMemoryRepository();
        repository.Seed(user);
        repository.Seed(
            new Weight { ID = Guid.NewGuid(), UserID = user.ID, WeightValue = 80, Timestamp = new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero) },
            new Weight { ID = Guid.NewGuid(), UserID = user.ID, WeightValue = 78.5, Timestamp = new DateTimeOffset(2024, 6, 1, 8, 0, 0, TimeSpan.Zero) },
            new Weight { ID = Guid.NewGuid(), UserID = Guid.NewGuid(), WeightValue = 95, Timestamp = new DateTimeOffset(2024, 7, 1, 8, 0, 0, TimeSpan.Zero) });
        var viewModel = Create(repository);

        await viewModel.EnsureLoadedAsync();

        // Note the German culture: the view model formats for the German UI.
        Assert.Equal("78,5", viewModel.WeightText);
    }

    [Fact]
    public async Task Load_WithoutAProfile_AnnouncesThatSavingCreatesIt()
    {
        var viewModel = Create(new InMemoryRepository());

        await viewModel.EnsureLoadedAsync();

        Assert.Contains("noch kein Profil", viewModel.StatusMessage);
        Assert.Contains("Noch keine Angaben", viewModel.BodySummaryText);
    }

    [Fact]
    public async Task EnsureLoaded_RunsOnlyOnce()
    {
        var repository = new InMemoryRepository();
        repository.Seed(new User { ID = Guid.NewGuid(), Name = "Chris" });
        var viewModel = Create(repository);

        await viewModel.EnsureLoadedAsync();
        viewModel.Name = "Von Hand geändert";
        await viewModel.EnsureLoadedAsync();

        Assert.Equal("Von Hand geändert", viewModel.Name);
    }

    [Fact]
    public async Task Save_CreatesTheProfileAndOneWeightEntry()
    {
        var repository = new InMemoryRepository();
        var dialog = new FakeDialogService();
        var viewModel = Create(repository, dialog);
        await viewModel.EnsureLoadedAsync();

        viewModel.Name = "Chris";
        viewModel.Email = "chris@example.com";
        viewModel.HeightText = "182";
        viewModel.WeightText = "78,5";
        await viewModel.SaveCommand.ExecuteAsync(null);

        var user = Assert.Single(repository.Rows<User>());
        Assert.Equal("Chris", user.Name);
        Assert.Equal(182, user.Height);

        var weight = Assert.Single(repository.Rows<Weight>());
        Assert.Equal(78.5, weight.WeightValue);
        Assert.Equal(user.ID, weight.UserID);
        Assert.Equal("Profil gespeichert.", viewModel.StatusMessage);
        Assert.Contains("Profil gespeichert", dialog.Toasts);
    }

    [Fact]
    public async Task Save_UpdatesTheExistingProfileInsteadOfAddingANewOne()
    {
        var user = new User { ID = Guid.NewGuid(), Name = "Alt", Email = "alt@example.com", Height = 180 };
        var repository = new InMemoryRepository();
        repository.Seed(user);
        var viewModel = Create(repository);
        await viewModel.EnsureLoadedAsync();

        viewModel.Name = "Neu";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Single(repository.Rows<User>());
        Assert.Equal("Neu", Assert.Single(repository.Rows<User>()).Name);
        Assert.Contains(repository.Calls, call => call.Operation == "Update");
        Assert.DoesNotContain(repository.Calls, call => call is { Operation: "Add", EntityType: var type } && type == typeof(User));
    }

    [Fact]
    public async Task Save_RepeatingTheSameWeight_DoesNotPileUpHistoryEntries()
    {
        var user = new User { ID = Guid.NewGuid(), Name = "Chris", Height = 182 };
        var repository = new InMemoryRepository();
        repository.Seed(user);
        repository.Seed(new Weight
        {
            ID = Guid.NewGuid(),
            UserID = user.ID,
            WeightValue = 78.5,
            Timestamp = DateTimeOffset.Now,
        });
        var viewModel = Create(repository);
        await viewModel.EnsureLoadedAsync();

        await viewModel.SaveCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Single(repository.Rows<Weight>());
    }

    [Fact]
    public async Task Save_AChangedWeight_AddsAHistoryEntry()
    {
        var user = new User { ID = Guid.NewGuid(), Name = "Chris", Height = 182 };
        var repository = new InMemoryRepository();
        repository.Seed(user);
        repository.Seed(new Weight
        {
            ID = Guid.NewGuid(),
            UserID = user.ID,
            WeightValue = 80,
            Timestamp = DateTimeOffset.Now,
        });
        var viewModel = Create(repository);
        await viewModel.EnsureLoadedAsync();

        viewModel.WeightText = "79";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(2, repository.Rows<Weight>().Count);
    }

    [Fact]
    public async Task Save_WithoutALoadedProfile_StillWritesASingleProfile()
    {
        // Mimics the fast tap: the caller never awaited the load, the save command must do it.
        var repository = new InMemoryRepository();
        var viewModel = Create(repository);

        viewModel.Name = "Chris";
        viewModel.HeightText = "182";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Single(repository.Rows<User>());
        Assert.Equal("Chris", Assert.Single(repository.Rows<User>()).Name);
    }

    [Theory]
    [InlineData("49")]
    [InlineData("261")]
    [InlineData("abc")]
    public async Task Save_RejectsAnImpossibleHeight(string height)
    {
        var repository = new InMemoryRepository();
        var dialog = new FakeDialogService();
        var viewModel = Create(repository, dialog);
        await viewModel.EnsureLoadedAsync();

        viewModel.HeightText = height;
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(repository.Rows<User>());
        Assert.Contains("Größe", Assert.Single(dialog.Alerts).Title);
    }

    [Theory]
    [InlineData("19")]
    [InlineData("401")]
    [InlineData("schwer")]
    public async Task Save_RejectsAnImpossibleWeight(string weight)
    {
        var repository = new InMemoryRepository();
        var dialog = new FakeDialogService();
        var viewModel = Create(repository, dialog);
        await viewModel.EnsureLoadedAsync();

        viewModel.HeightText = "182";
        viewModel.WeightText = weight;
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(repository.Rows<User>());
        Assert.Contains("Gewicht", Assert.Single(dialog.Alerts).Title);
    }

    [Fact]
    public async Task Save_AnEmptyForm_KeepsTheStoredValues()
    {
        var user = new User { ID = Guid.NewGuid(), Name = "Chris", Email = "chris@example.com", Height = 182 };
        var repository = new InMemoryRepository();
        repository.Seed(user);
        var viewModel = Create(repository);
        await viewModel.EnsureLoadedAsync();

        viewModel.Name = "   ";
        viewModel.Email = string.Empty;
        viewModel.HeightText = string.Empty;
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Chris", user.Name);
        Assert.Equal("chris@example.com", user.Email);
        Assert.Null(user.Height);
    }

    [Fact]
    public async Task BodySummary_ReportsHeightWeightAndBmi()
    {
        var viewModel = Create(new InMemoryRepository());
        await viewModel.EnsureLoadedAsync();

        viewModel.HeightText = "180";
        viewModel.WeightText = "80";

        Assert.Contains("Größe 180 cm", viewModel.BodySummaryText);
        Assert.Contains("Gewicht 80 kg", viewModel.BodySummaryText);
        Assert.Contains("BMI 24,7 (Normalgewicht)", viewModel.BodySummaryText);
    }

    [Fact]
    public async Task BodySummary_WithOnlyHeight_SkipsTheBmi()
    {
        var viewModel = Create(new InMemoryRepository());
        await viewModel.EnsureLoadedAsync();

        viewModel.HeightText = "180";
        viewModel.WeightText = string.Empty;

        Assert.Contains("Größe 180 cm", viewModel.BodySummaryText);
        Assert.DoesNotContain("BMI", viewModel.BodySummaryText);
    }

    [Fact]
    public async Task Cancel_NavigatesBack()
    {
        var navigation = new FakeNavigationService();
        var viewModel = Create(new InMemoryRepository(), navigation: navigation);

        await viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Equal(1, navigation.NavigateBackCalls);
    }

    [Fact]
    public async Task ShowWeightHint_ExplainsTheHistory()
    {
        var dialog = new FakeDialogService();
        var viewModel = Create(new InMemoryRepository(), dialog);

        await viewModel.ShowWeightHintCommand.ExecuteAsync(null);

        Assert.Contains("Verlaufseintrag", Assert.Single(dialog.Alerts).Message);
    }

    private static ProfilePageViewModel Create(
        InMemoryRepository repository,
        FakeDialogService? dialogService = null,
        FakeNavigationService? navigation = null) =>
        new(repository, dialogService ?? new FakeDialogService(), navigation ?? new FakeNavigationService());
}
