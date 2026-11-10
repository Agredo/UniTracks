namespace UniTracks.Services.Settings;

/// <summary>The activity type explicitly selected for the next recording.</summary>
public interface IRecordingSettings
{
    /// <summary>The type selected for recording and restored when a recording page opens.</summary>
    /// <remarks>Serialized GUID, or empty when no type has been selected yet.</remarks>
    string LastTripTypeId { get; set; }
}
