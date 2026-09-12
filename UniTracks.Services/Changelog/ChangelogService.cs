using System.Reflection;

namespace UniTracks.Services.Changelog;

/// <summary>
/// Loads the embedded <c>Changelog/changelog.json</c> catalog. The file is maintained by hand, one
/// entry per released version; see <see cref="ChangelogCatalog"/> for the accepted shape.
/// </summary>
public sealed class ChangelogService : IChangelogService
{
    private const string ResourceFileName = "changelog.json";

    private readonly Lazy<IReadOnlyList<ChangelogRelease>> releases;

    public ChangelogService()
    {
        releases = new Lazy<IReadOnlyList<ChangelogRelease>>(LoadReleases);
    }

    public IReadOnlyList<ChangelogRelease> Releases => releases.Value;

    public ChangelogRelease? Find(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        foreach (var release in Releases)
        {
            if (string.Equals(release.Version, version, StringComparison.OrdinalIgnoreCase))
            {
                return release;
            }
        }

        // "0.4" and "0.4.0" describe the same build, so fall back to a numeric comparison.
        foreach (var release in Releases)
        {
            if (ChangelogVersion.IsSame(release.Version, version))
            {
                return release;
            }
        }

        return null;
    }

    public IReadOnlyList<ChangelogRelease> GetSince(string? version)
    {
        if (string.IsNullOrWhiteSpace(version) || !ChangelogVersion.IsValid(version))
        {
            return Releases;
        }

        return [.. Releases.Where(release => ChangelogVersion.Compare(release.Version, version) >= 0)];
    }

    private static IReadOnlyList<ChangelogRelease> LoadReleases()
    {
        var assembly = typeof(ChangelogService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return [];
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return [];
        }

        using var reader = new StreamReader(stream);
        return ChangelogCatalog.Parse(reader.ReadToEnd());
    }
}
