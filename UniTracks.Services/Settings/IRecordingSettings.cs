namespace UniTracks.Services.Settings;

/// <summary>The activity type explicitly selected for the next recording.</summary>
public interface IRecordingSettings
{
    /// <summary>The type selected for recording and restored when a recording page opens.</summary>
    /// <remarks>Serialized GUID, or empty when no type has been selected yet.</remarks>
    string LastTripTypeId { get; set; }

    /// <summary>
    /// The types the runner picked most recently, newest first — the history behind the quick-pick row
    /// on the record page.
    /// </summary>
    /// <remarks>
    /// Stored independently of the recorded trips: a type that was picked but not recorded yet has to
    /// stay one tap away as well, so the history may not be derived from the trip collection alone.
    /// </remarks>
    IReadOnlyList<Guid> RecentTripTypeIds { get; set; }
}
