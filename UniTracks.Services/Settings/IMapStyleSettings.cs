namespace UniTracks.Services.Settings;

/// <summary>Background map styles the app offers for the recorded tracks.</summary>
public enum MapStyleKind
{
    /// <summary>OpenStreetMap standard.</summary>
    Standard,

    /// <summary>Carto "light": pale background, the track stands out the most.</summary>
    Light,

    /// <summary>OpenTopoMap: contour lines and terrain, useful on trails.</summary>
    Topographic,
}

/// <summary>
/// The map style chosen by the user. Only the tile source behind the route changes; the recorded
/// points, distances and statistics are unaffected.
/// </summary>
public interface IMapStyleSettings
{
    MapStyleKind Style { get; set; }
}
