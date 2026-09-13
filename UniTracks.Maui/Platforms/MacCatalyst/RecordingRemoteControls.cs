using UniTracks.Services.Location;

namespace UniTracks.Maui;

/// <summary>
/// Desktop fallback: a Mac has no lock screen to control a recording from, so the app's own buttons
/// stay the only control. The type exists so the app can resolve the same dependency on every
/// platform.
/// </summary>
public class RecordingRemoteControls : IRecordingRemoteControls
{
    public event EventHandler? PauseRequested
    {
        add { }
        remove { }
    }

    public event EventHandler? ResumeRequested
    {
        add { }
        remove { }
    }

    public event EventHandler? StopRequested
    {
        add { }
        remove { }
    }

    public void Update(RecordingRemoteState state, TimeSpan elapsed)
    {
    }
}
