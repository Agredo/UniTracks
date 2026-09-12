namespace UniTracks.Services.Changelog;

/// <summary>
/// How a single entry of a release note should be presented.
/// </summary>
public enum ChangelogChangeKind
{
    /// <summary>A brand new capability.</summary>
    Feature,

    /// <summary>An existing capability that works differently now.</summary>
    Improvement,

    /// <summary>A bug that no longer happens.</summary>
    Fix,
}

/// <summary>One bullet point of a release note.</summary>
public sealed class ChangelogChange
{
    public required string Text { get; init; }

    public ChangelogChangeKind Kind { get; init; } = ChangelogChangeKind.Improvement;
}

/// <summary>Everything that shipped in one app version.</summary>
public sealed class ChangelogRelease
{
    /// <summary>The app version string this entry belongs to.</summary>
    public required string Version { get; init; }

    /// <summary>Release date in <c>yyyy-MM-dd</c> form, empty when unknown.</summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>Short human readable headline, e.g. "Laufrichtung &amp; Versionshinweise".</summary>
    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<ChangelogChange> Changes { get; init; } = [];
}
