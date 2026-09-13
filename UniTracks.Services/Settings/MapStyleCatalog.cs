namespace UniTracks.Services.Settings;

/// <summary>Everything needed to build a Mapsui/BruTile tile layer for one map style.</summary>
public record MapTileSourceDefinition(
    string Name,
    string UrlTemplate,
    string Attribution,
    string AttributionUrl,
    IReadOnlyList<string> ServerNodes);

/// <summary>One entry of the map style picker: value, label and the sentence below it.</summary>
public record MapStyleOption(MapStyleKind Kind, string DisplayName, string Description)
{
    /// <summary>What the picker shows without an item template.</summary>
    public override string ToString() => DisplayName;
}

/// <summary>
/// The map styles offered in the settings, with their tile sources and the user-facing labels.
/// The catalog is platform-neutral so it can be unit tested; only the persistence of the selection
/// lives in the MAUI layer.
/// </summary>
public static class MapStyleCatalog
{
    /// <summary>OpenStreetMap standard: what the app used before the style could be chosen.</summary>
    public const MapStyleKind Default = MapStyleKind.Standard;

    public static IReadOnlyList<MapStyleKind> All { get; } =
        [MapStyleKind.Standard, MapStyleKind.Light, MapStyleKind.Topographic];

    /// <summary>The picker entries in the order of <see cref="All"/>.</summary>
    public static IReadOnlyList<MapStyleOption> Options { get; } =
        [.. All.Select(kind => new MapStyleOption(kind, DisplayName(kind), Description(kind)))];

    /// <summary>The option for a style, so a view model can show the stored selection.</summary>
    public static MapStyleOption Option(MapStyleKind kind) =>
        Options.FirstOrDefault(option => option.Kind == kind) ?? Options[0];

    public static string DisplayName(MapStyleKind kind) => kind switch
    {
        MapStyleKind.Light => "Hell (Carto)",
        MapStyleKind.Topographic => "Topografisch (OpenTopoMap)",
        _ => "Standard (OpenStreetMap)",
    };

    public static string Description(MapStyleKind kind) => kind switch
    {
        MapStyleKind.Light => "Sehr heller Hintergrund – die Strecke hebt sich am stärksten ab.",
        MapStyleKind.Topographic => "Mit Höhenlinien und Gelände – praktisch für Wanderungen.",
        _ => "Der klassische Straßen- und Wegelayer von OpenStreetMap.",
    };

    /// <summary>Short hint shown under the picker, including the attribution of the active style.</summary>
    public static string AttributionHint(MapStyleKind kind) => $"Kartendaten: {TileSource(kind).Attribution}";

    public static MapTileSourceDefinition TileSource(MapStyleKind kind) => kind switch
    {
        MapStyleKind.Light => new MapTileSourceDefinition(
            "Carto Light",
            "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png",
            "© OpenStreetMap contributors, © CARTO",
            "https://carto.com/attributions",
            ["a", "b", "c", "d"]),

        MapStyleKind.Topographic => new MapTileSourceDefinition(
            "OpenTopoMap",
            "https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png",
            "© OpenStreetMap contributors, SRTM | © OpenTopoMap (CC-BY-SA)",
            "https://opentopomap.org",
            ["a", "b", "c"]),

        _ => new MapTileSourceDefinition(
            "OpenStreetMap",
            "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
            "© OpenStreetMap contributors",
            "https://www.openstreetmap.org/copyright",
            []),
    };

    /// <summary>Writes the enum name so a renamed style never silently changes the stored value.</summary>
    public static string ToStorageValue(MapStyleKind kind) => kind.ToString();

    /// <summary>Falls back to <see cref="Default"/> for unknown or missing stored values.</summary>
    public static MapStyleKind Parse(string? value) =>
        Enum.TryParse<MapStyleKind>(value, ignoreCase: true, out var kind) && Enum.IsDefined(kind)
            ? kind
            : Default;
}
