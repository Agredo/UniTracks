using UniTracks.Models.Trip;
using UniTracks.Tests.TestSupport;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// The type picker used to match only the English catalog names, so a German search term such as
/// "Radfahren" or "Fahrrad" returned an empty list even though the app is German throughout.
/// </summary>
public sealed class TripTypeSearchPopupViewModelTests
{
    private static TripType Type(string identifier, string name) => new()
    {
        ID = Guid.NewGuid(),
        Identifier = identifier,
        Name = name,
        Description = string.Empty,
        Category = "cycling",
    };

    private static InMemoryRepository Catalog()
    {
        var repository = new InMemoryRepository();
        repository.Seed(
            Type("run", "Laufen"),
            Type("cycling", "Radfahren"),
            Type("mountainbiking", "Mountainbiken"),
            Type("swimming", "Schwimmen"),
            Type("dogwalk", "Gassi gehen"));
        return repository;
    }

    private static async Task<TripTypeSearchPopupViewModel> LoadedAsync(InMemoryRepository repository)
    {
        var viewModel = new TripTypeSearchPopupViewModel(repository);

        // The constructor kicks the load off without awaiting it.
        for (int attempt = 0; attempt < 100 && viewModel.Types.Count == 0; attempt++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.NotEmpty(viewModel.Types);
        return viewModel;
    }

    [Fact]
    public async Task Load_ShowsTheGermanCatalogSortedByName()
    {
        var viewModel = await LoadedAsync(Catalog());

        Assert.Equal(
            new[] { "Gassi gehen", "Laufen", "Mountainbiken", "Radfahren", "Schwimmen" },
            viewModel.Types.Select(type => type.Name));
    }

    [Theory]
    [InlineData("Radfahren", "Radfahren")]
    [InlineData("joggen", "Laufen")]
    [InlineData("hund", "Gassi gehen")]
    [InlineData("run", "Laufen")]
    [InlineData("radfahren", "Radfahren")]
    public async Task Search_FindsTheTypeByGermanNameSynonymOrIdentifier(string term, string expected)
    {
        var viewModel = await LoadedAsync(Catalog());

        viewModel.SearchText = term;

        Assert.Equal(expected, Assert.Single(viewModel.Types).Name);
    }

    [Fact]
    public async Task Search_FahrradFindsEveryBikeType()
    {
        var viewModel = await LoadedAsync(Catalog());

        viewModel.SearchText = "Fahrrad";

        Assert.Equal(
            new[] { "Mountainbiken", "Radfahren" },
            viewModel.Types.Select(type => type.Name));
    }

    [Fact]
    public async Task Search_WithNoMatchShowsNothing()
    {
        var viewModel = await LoadedAsync(Catalog());

        viewModel.SearchText = "Segelflugzeug";

        Assert.Empty(viewModel.Types);
    }

    [Fact]
    public async Task Search_ClearedShowsTheWholeCatalogAgain()
    {
        var viewModel = await LoadedAsync(Catalog());

        viewModel.SearchText = "Fahrrad";
        Assert.Equal(2, viewModel.Types.Count);

        viewModel.SearchText = string.Empty;
        Assert.Equal(5, viewModel.Types.Count);
    }
}
