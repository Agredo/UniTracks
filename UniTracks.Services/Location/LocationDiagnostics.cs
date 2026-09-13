using System.Text;

namespace UniTracks.Services.Location;

/// <summary>
/// Append-only diagnostic log for the location pipeline
/// (<c>LocalApplicationData/UniTracks/location.log</c>).
///
/// Background capture can stop without any exception on either platform: iOS suspends the app or
/// ends the update session, Android kills the foreground service. A silent stop therefore leaves no
/// trace at all — no crash log, no exception, just missing points. Everything the platform tells us
/// (authorization changes, <c>didFailWithError:</c>, deferred deliveries) and every gap between two
/// fixes is written here so a lost part of a trip can be explained afterwards instead of guessed.
/// </summary>
public static class LocationDiagnostics
{
    private const long MaxBytes = 512 * 1024;

    private static readonly object Sync = new();

    private static string? filePath;

    /// <summary>Path of the log file, or an empty string when no path could be resolved.</summary>
    public static string FilePath => filePath ??= BuildPath();

    /// <summary>Most recent entry, so the UI can surface the last event without reading the file.</summary>
    public static string? LastEntry { get; private set; }

    /// <summary>Appends a timestamped line to the location log. Never throws.</summary>
    public static void Write(string message)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        LastEntry = line;

        try
        {
            lock (Sync)
            {
                var file = FilePath;
                if (file.Length == 0)
                {
                    return;
                }

                if (File.Exists(file) && new FileInfo(file).Length > MaxBytes)
                {
                    File.Delete(file);
                }

                File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Diagnostics must never break the recording.
        }
    }

    private static string BuildPath()
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UniTracks");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "location.log");
        }
        catch
        {
            try
            {
                var directory = Path.Combine(Path.GetTempPath(), "UniTracks");
                Directory.CreateDirectory(directory);
                return Path.Combine(directory, "location.log");
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
