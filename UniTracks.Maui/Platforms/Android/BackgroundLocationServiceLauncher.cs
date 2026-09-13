using Android.Content;
using AndroidX.Core.Content;
using UniTracks.Services.Data;

namespace UniTracks.Maui;

/// <summary>
/// Hands an intent to <see cref="BackgroundLocationService"/> for every platform caller.
///
/// Android 12+ refuses to start a foreground service from the background. The paths that need this
/// (pausing, resuming and refreshing the state of a recording) all act on a service that is already
/// running, so a plain <c>StartService</c> is used as fallback - and a failure is only logged, because
/// the lock-screen buttons reach the service directly through its notification and keep working.
/// </summary>
internal static class BackgroundLocationServiceLauncher
{
    public static void Send(Context context, Intent intent)
    {
        try
        {
            ContextCompat.StartForegroundService(context, intent);
            return;
        }
        catch (Exception ex)
        {
            LocationDiagnostics.Write(
                $"Android StartForegroundService({Describe(intent)}) nicht erlaubt: {ex.Message}");
        }

        try
        {
            context.StartService(intent);
        }
        catch (Exception ex)
        {
            LocationDiagnostics.Write($"Android StartService({Describe(intent)}) fehlgeschlagen: {ex.Message}");
        }
    }

    private static string Describe(Intent intent) => intent.Action ?? "Start";

    /// <summary>
    /// Refreshes a service that is already running. This is the path for state updates: a plain start
    /// is exempt from the background restrictions while the service is live (unlike
    /// <see cref="Send"/>, because "the service is already started"), and it does not arm the
    /// "must call StartForeground within a few seconds" timer that StartForegroundService sets up.
    /// </summary>
    public static void SendToRunningService(Context context, Intent intent)
    {
        try
        {
            context.StartService(intent);
            return;
        }
        catch (Exception ex)
        {
            LocationDiagnostics.Write($"Android StartService({Describe(intent)}) fehlgeschlagen: {ex.Message}");
        }

        // The service is not running after all, so it has to be started (and promoted) as usual.
        Send(context, intent);
    }
}
