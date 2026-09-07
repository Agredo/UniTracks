namespace UniTracks.Models.Stats;

/// <summary>
/// One bar/point in a chart. Shared between ViewModels (which build the series)
/// and the chart custom controls (which render it) so both stay MVVM-friendly.
/// </summary>
public record ChartEntry
{
    /// <summary>Short axis label, e.g. a week start date or category name.</summary>
    public string Label { get; init; } = string.Empty;

    public double Value { get; init; }

    /// <summary>Render this entry accentuated (e.g. the current week).</summary>
    public bool IsHighlighted { get; init; }
}
