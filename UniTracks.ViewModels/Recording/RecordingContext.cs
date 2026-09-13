using UniTracks.Models.Trip;

namespace UniTracks.ViewModels.Recording;

/// <summary>
/// Everything the user configured for the next (or the running) recording. Today that is only the
/// trip type; shoes, heart-rate sources and similar recording attributes plug in here later without
/// restructuring the page — each one becomes another entry of the record page's context bar.
/// </summary>
public sealed class RecordingContext
{
    /// <summary>The activity of the trip. Drives categorisation, comparison and gamification.</summary>
    public TripType? TripType { get; set; }
}
