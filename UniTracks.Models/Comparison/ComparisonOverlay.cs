using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Models.Comparison;

/// <summary>
/// One track of a multi-trip comparison, ready to be drawn: the points plus the colour that stands
/// for this trip everywhere on the comparison page (map, chart legend, splits).
/// </summary>
public sealed record ComparisonTrack
{
    public required string Name { get; init; }

    public required string ColorHex { get; init; }

    public required IReadOnlyList<LocationModel> Locations { get; init; }
}

/// <summary>
/// One pace curve of a multi-trip comparison. All curves share the same distance axis, so equal
/// indices always mean the same kilometre.
/// </summary>
public sealed record ComparisonCurve
{
    public required string Name { get; init; }

    public required string ColorHex { get; init; }

    /// <summary>Climb-adjusted pace per kilometre, in seconds. 0 for a kilometre that is missing.</summary>
    public required IReadOnlyList<double> Values { get; init; }
}
