using UniTracks.Services.Settings;

namespace UniTracks.Tests.Settings;

/// <summary>
/// Tests for <see cref="MapStyleCatalog"/>. The settings page offers the picker, but the map builds
/// its tile layer from the same catalog: a style without a URL template would leave the user with a
/// blank map, and an unknown stored value must fall back to the default instead of throwing.
/// </summary>
public sealed class MapStyleCatalogTests
{
    public static TheoryData<MapStyleKind> AllStyles()
    {
        var data = new TheoryData<MapStyleKind>();
        foreach (var kind in MapStyleCatalog.All)
        {
            data.Add(kind);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllStyles))]
    public void TileSource_IsComplete(MapStyleKind kind)
    {
        var source = MapStyleCatalog.TileSource(kind);

        Assert.False(string.IsNullOrWhiteSpace(source.Name));
        Assert.Contains("{z}", source.UrlTemplate);
        Assert.Contains("{x}", source.UrlTemplate);
        Assert.Contains("{y}", source.UrlTemplate);
        Assert.False(string.IsNullOrWhiteSpace(source.Attribution));
    }

    [Fact]
    public void TileSource_WithServerNodes_ContainsThePlaceholder()
    {
        var source = MapStyleCatalog.TileSource(MapStyleKind.Light);

        Assert.NotEmpty(source.ServerNodes);
        Assert.Contains("{s}", source.UrlTemplate);
        Assert.Contains("a", source.ServerNodes);
    }

    [Fact]
    public void TileSource_WithoutServerNodes_HasNoPlaceholder()
    {
        var source = MapStyleCatalog.TileSource(MapStyleKind.Standard);

        // {s} without server nodes would make HttpTileSource throw.
        Assert.Empty(source.ServerNodes);
        Assert.DoesNotContain("{s}", source.UrlTemplate);
    }

    [Fact]
    public void EveryStyle_HasItsOwnTileSource()
    {
        var names = MapStyleCatalog.All.Select(kind => MapStyleCatalog.TileSource(kind).Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(AllStyles))]
    public void DisplayNameAndDescription_AreSet(MapStyleKind kind)
    {
        Assert.False(string.IsNullOrWhiteSpace(MapStyleCatalog.DisplayName(kind)));
        Assert.False(string.IsNullOrWhiteSpace(MapStyleCatalog.Description(kind)));
        Assert.Contains(MapStyleCatalog.TileSource(kind).Attribution, MapStyleCatalog.AttributionHint(kind));
    }

    [Theory]
    [MemberData(nameof(AllStyles))]
    public void StorageValue_RoundTrips(MapStyleKind kind)
    {
        var stored = MapStyleCatalog.ToStorageValue(kind);

        Assert.Equal(kind.ToString(), stored);
        Assert.Equal(kind, MapStyleCatalog.Parse(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NichtVorhanden")]
    [InlineData("42")]
    public void Parse_UnknownValue_FallsBackToTheDefault(string? stored) =>
        Assert.Equal(MapStyleCatalog.Default, MapStyleCatalog.Parse(stored));

    [Fact]
    public void Parse_IgnoresTheCaseOfTheStoredValue() =>
        Assert.Equal(MapStyleKind.Topographic, MapStyleCatalog.Parse("topographic"));

    [Fact]
    public void Options_CoverEveryStyleInOrder()
    {
        Assert.Equal(MapStyleCatalog.All, MapStyleCatalog.Options.Select(option => option.Kind));
        Assert.All(MapStyleCatalog.Options, option =>
        {
            Assert.Equal(MapStyleCatalog.DisplayName(option.Kind), option.DisplayName);
            Assert.Equal(MapStyleCatalog.Description(option.Kind), option.Description);
        });
    }

    [Fact]
    public void Option_FindsTheEntryOfAStyle()
    {
        var option = MapStyleCatalog.Option(MapStyleKind.Light);

        Assert.Equal(MapStyleKind.Light, option.Kind);
        Assert.Contains("Hell", option.DisplayName);
    }

    [Fact]
    public void Option_IsTheSameInstanceAsInTheList()
    {
        // The picker compares by reference, so the view model has to bind to these very objects.
        Assert.Same(
            MapStyleCatalog.Options.Single(option => option.Kind == MapStyleKind.Topographic),
            MapStyleCatalog.Option(MapStyleKind.Topographic));
    }

    [Fact]
    public void Option_ShowsTheDisplayNameWhenThereIsNoItemTemplate()
    {
        var option = MapStyleCatalog.Option(MapStyleKind.Standard);

        Assert.Equal(option.DisplayName, option.ToString());
    }

    [Fact]
    public void Default_IsTheOpenStreetMapStyle() => Assert.Equal(MapStyleKind.Standard, MapStyleCatalog.Default);
}
