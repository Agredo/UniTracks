namespace UniTracks.Services.Location;

/// <summary>
/// What a platform's lock-screen surface shows for the running recording.
/// </summary>
public enum RecordingRemoteState
{
    /// <summary>No recording: the lock-screen surface is removed.</summary>
    Stopped,

    /// <summary>Capture runs. The surface offers pause and stop.</summary>
    Recording,

    /// <summary>Capture is suspended, the trip stays open. The surface offers resume and stop.</summary>
    Paused,
}

/// <summary>
/// Lock-screen controls for a running recording (Android notification actions, iOS notification
/// actions). The platform layer reports taps back through the events and shows the state the app
/// publishes, so the lock screen and the in-app buttons drive the same recording.
/// </summary>
public interface IRecordingRemoteControls
{
    /// <summary>Raised when the user pauses the recording from the lock screen.</summary>
    event EventHandler? PauseRequested;

    /// <summary>Raised when the user continues a paused recording from the lock screen.</summary>
    event EventHandler? ResumeRequested;

    /// <summary>Raised when the user ends the recording from the lock screen.</summary>
    event EventHandler? StopRequested;

    /// <summary>
    /// Publishes the recording state. <paramref name="elapsed"/> is the time already recorded, so a
    /// platform that shows a running clock can continue it after a pause instead of starting at zero.
    /// </summary>
    void Update(RecordingRemoteState state, TimeSpan elapsed);
}
