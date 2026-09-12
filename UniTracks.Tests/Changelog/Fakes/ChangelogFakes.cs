using UniTracks.Services.Changelog;

namespace UniTracks.Tests.Changelog.Fakes;

/// <summary>Changelog catalog stub: each test decides which releases exist.</summary>
internal sealed class FakeChangelogService : IChangelogService
{
    public List<ChangelogRelease> Items { get; } = new();

    public IReadOnlyList<ChangelogRelease> Releases => Items;

    public ChangelogRelease? Find(string? version) =>
        Items.FirstOrDefault(release => ChangelogVersion.IsSame(release.Version, version));

    public IReadOnlyList<ChangelogRelease> GetSince(string? version) =>
    [
        .. Items.Where(release => string.IsNullOrWhiteSpace(version)
            || ChangelogVersion.Compare(release.Version, version) >= 0),
    ];
}

/// <summary>Remembers the last shown version in memory instead of in platform preferences.</summary>
internal sealed class FakeChangelogState : IChangelogState
{
    public string? LastSeenVersion { get; set; }
}
