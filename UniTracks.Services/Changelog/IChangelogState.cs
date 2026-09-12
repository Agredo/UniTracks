namespace UniTracks.Services.Changelog;

/// <summary>
/// Remembers which release notes the user has already been shown, so the "what's new" popup only
/// appears once per installed version.
/// </summary>
public interface IChangelogState
{
    /// <summary>The app version whose release notes were shown last, or <c>null</c> on a fresh install.</summary>
    string? LastSeenVersion { get; set; }
}
