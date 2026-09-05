using System.Text;

namespace UniTracks.Maui.Views;

/// <summary>
/// Minimal file-backed exception log used to capture the cause of UI crashes that
/// would otherwise vanish as a native WinUI stowed exception (<c>0xc000027b</c>).
/// Writes are appended and truncated if the file grows too large.
/// </summary>
public static class CrashLog
{
    private const long MaxBytes = 512 * 1024;

    private static readonly string FilePath = BuildPath();

    private static readonly object Sync = new();

    private static string BuildPath()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UniTracks");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "crash.log");
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "UniTracks", "crash.log");
        }
    }

    /// <summary>Appends a timestamped line to the crash log.</summary>
    public static void Write(string message)
    {
        try
        {
            lock (Sync)
            {
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxBytes)
                {
                    File.WriteAllText(FilePath, string.Empty);
                }

                var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(FilePath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never cause a crash itself.
        }
    }
}
