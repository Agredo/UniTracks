namespace UniTracks.Games.Shared.Persistence;

/// <summary>One recorded trip's contribution to the coin economy.</summary>
public record TripActivity
{
    /// <summary>Distance in kilometers.</summary>
    public double DistanceKm { get; init; }

    /// <summary>
    /// Trip start. Drives the time-based games (the base camp's rolling activity window and
    /// its decay); <see cref="DateTimeOffset.MinValue"/> when the caller does not supply one.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>Category from the trip-type seed catalog (e.g. "running", "cycling").</summary>
    public string? Category { get; init; }

    /// <summary>Trip-type identifier (e.g. "run", "ebikeride") — used for motor-assisted overrides.</summary>
    public string? Identifier { get; init; }
}
