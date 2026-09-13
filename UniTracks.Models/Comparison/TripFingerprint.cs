using System.ComponentModel.DataAnnotations;

namespace UniTracks.Models.Comparison;

/// <summary>
/// A compact, searchable summary of a trip's route shape and demands, stored next to the trip so a
/// comparison never has to load the (heavy) GPS point lists.
/// <para>
/// One fingerprint per trip. It is derived data: it can always be rebuilt from the trip's locations,
/// which is what <see cref="Version"/> is for — when the derivation changes, stale fingerprints are
/// recomputed in the background instead of the app showing wrong matches.
/// </para>
/// </summary>
public record TripFingerprint
{
    /// <summary>Owned trip; doubles as the primary key so a trip can hold at most one fingerprint.</summary>
    [Key]
    public Guid TripID { get; set; }

    /// <summary>Derivation version. Bump when the fingerprint layout or its algorithms change.</summary>
    public int Version { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public Guid? TripTypeId { get; set; }

    /// <summary>
    /// Coarse sport family ("running", "cycling", …) copied from the trip type so a comparison never
    /// has to join the catalogue — on iOS the document store does not resolve navigations at all.
    /// Empty means the trip was recorded without a type.
    /// </summary>
    public string TripCategory { get; set; } = string.Empty;

    /// <summary>
    /// Fine trip type ("run", "trailrun", "walk", …). Walk and Run share the category "running", so
    /// the category alone would happily file a dog walk as a run on the same route.
    /// </summary>
    public string TripIdentifier { get; set; } = string.Empty;

    /// <summary>Number of raw GPS fixes the fingerprint was derived from.</summary>
    public int PointCount { get; set; }

    public double DistanceMeters { get; set; }

    public double ElapsedSeconds { get; set; }

    public double MovingSeconds { get; set; }

    public double AverageSpeedMetersPerSecond { get; set; }

    public double MaxSpeedMetersPerSecond { get; set; }

    public double ElevationGainMeters { get; set; }

    public double MinAltitude { get; set; }

    public double MaxAltitude { get; set; }

    /// <summary>Latitude/longitude extent, used to reject far-away candidates before any geometry work.</summary>
    public double MinLatitude { get; set; }

    public double MaxLatitude { get; set; }

    public double MinLongitude { get; set; }

    public double MaxLongitude { get; set; }

    /// <summary>Mean position of the track; the coarse "where was this" handle.</summary>
    public double CenterLatitude { get; set; }

    public double CenterLongitude { get; set; }

    /// <summary>Route-precision cell of the first fix, so "other runs from the same spot" is a lookup.</summary>
    public string StartCell { get; set; } = string.Empty;

    public string EndCell { get; set; } = string.Empty;

    /// <summary>Coarse area cell of the first fix; survives parking 200 m further away.</summary>
    public string StartArea { get; set; } = string.Empty;

    public string EndArea { get; set; } = string.Empty;

    /// <summary>Every route-precision cell the track touches, deduplicated — the inverted index entry.</summary>
    public string[] RouteCells { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The track resampled to a fixed number of points spaced evenly by distance, so two recordings
    /// of one route line up point by point regardless of sampling rate.
    /// </summary>
    public double[] PolylineLatitudes { get; set; } = Array.Empty<double>();

    public double[] PolylineLongitudes { get; set; } = Array.Empty<double>();

    /// <summary>Cached equivalent speed; see the effort model. Never displayed, only ranked with.</summary>
    public double EffortScore { get; set; }

    /// <summary>Equivalent distance (length plus climbing penalty) that the effort score is based on.</summary>
    public double EquivalentDistanceMeters { get; set; }
}
