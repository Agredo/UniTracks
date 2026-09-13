namespace UniTracks.Services.Location;

/// <summary>
/// Shared timing for ending a recording.
///
/// After the platform is told to stop, iOS can still hand over locations that were queued while the
/// app was suspended. Detaching the delegate and finalising the trip immediately (the old behaviour)
/// dropped them: with <c>currentTrip</c> already cleared they either vanished or created a second,
/// unrelated trip. Capture is therefore stopped first, deliveries are accepted for
/// <see cref="Grace"/>, and only then is the trip finalised.
/// </summary>
public static class LocationDrain
{
    /// <summary>How long queued platform deliveries are still accepted after stopping capture.</summary>
    public static TimeSpan Grace { get; } = TimeSpan.FromSeconds(4);
}
