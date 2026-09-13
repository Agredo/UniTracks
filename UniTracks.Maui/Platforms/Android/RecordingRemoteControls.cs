using Android.App;
using Android.Content;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using UniTracks.Services.Location;

namespace UniTracks.Maui;

/// <summary>Command a lock-screen button sends into the running app.</summary>
internal enum RecordingRemoteCommand
{
    Pause,
    Resume,
    Stop,
}

/// <summary>
/// In-process bridge between the notification buttons and the app. The foreground service raises the
/// command, the app's <see cref="RecordingRemoteControls"/> forwards it to the recording page. Both
/// live in the same process, so no IPC or serialization is involved.
/// </summary>
internal static class RecordingRemoteCommands
{
    public static event EventHandler<RecordingRemoteCommand>? Received;

    public static void Raise(RecordingRemoteCommand command) => Received?.Invoke(null, command);
}

/// <summary>
/// Android lock-screen controls. The ongoing notification of the location foreground service is the
/// surface: <see cref="BackgroundLocationService"/> puts "Pausieren"/"Fortsetzen" and "Stoppen" on it
/// and feeds those taps back as commands, while <see cref="Update"/> mirrors the state the app holds
/// into the same notification so both stay in step.
/// </summary>
public class RecordingRemoteControls : IRecordingRemoteControls
{
    public event EventHandler? PauseRequested;

    public event EventHandler? ResumeRequested;

    public event EventHandler? StopRequested;

    public RecordingRemoteControls()
    {
        RecordingRemoteCommands.Received += OnCommandReceived;
    }

    public void Update(RecordingRemoteState state, TimeSpan elapsed)
    {
        Context context = Android.App.Application.Context;

        if (state == RecordingRemoteState.Stopped)
        {
            // Stopping already ends the service that owns the notification; cancelling makes sure no
            // leftover surface offers buttons for a recording that no longer runs.
            NotificationManagerCompat.From(context).Cancel(BackgroundLocationService.NotificationId);
            return;
        }

        var intent = new Intent(context, typeof(BackgroundLocationService));
        intent.SetAction(BackgroundLocationService.ActionStateUpdate);
        intent.PutExtra(BackgroundLocationService.ExtraState, (int)state);
        intent.PutExtra(BackgroundLocationService.ExtraElapsedMs, (long)elapsed.TotalMilliseconds);

        BackgroundLocationServiceLauncher.SendToRunningService(context, intent);
    }

    private void OnCommandReceived(object? sender, RecordingRemoteCommand command)
    {
        switch (command)
        {
            case RecordingRemoteCommand.Pause:
                PauseRequested?.Invoke(this, EventArgs.Empty);
                break;

            case RecordingRemoteCommand.Resume:
                ResumeRequested?.Invoke(this, EventArgs.Empty);
                break;

            case RecordingRemoteCommand.Stop:
                StopRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }
}
