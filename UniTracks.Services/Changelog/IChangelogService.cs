namespace UniTracks.Services.Changelog;

/// <summary>
/// Read access to the maintained release notes (<c>changelog.json</c>).
/// </summary>
public interface IChangelogService
{
    /// <summary>All releases, newest first.</summary>
    IReadOnlyList<ChangelogRelease> Releases { get; }

    /// <summary>
    /// The release matching <paramref name="version"/>. Returns <c>null</c> when the changelog has no
    /// entry for that version yet.
    /// </summary>
    ChangelogRelease? Find(string? version);

    /// <summary>
    /// The releases the user has not seen yet: everything newer than <paramref name="version"/>, plus
    /// <paramref name="version"/> itself. Returns everything when <paramref name="version"/> is
    /// unknown, and nothing when it is already the newest release.
    /// </summary>
    IReadOnlyList<ChangelogRelease> GetSince(string? version);
}
