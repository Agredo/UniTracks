using System.Globalization;
using UniTracks.Services.Changelog;

namespace UniTracks.ViewModels.Controls.Popups;

/// <summary>One bullet point in the "what's new" popup.</summary>
public sealed class ChangelogChangeViewModel
{
    public ChangelogChangeViewModel(ChangelogChange change)
    {
        Kind = change.Kind switch
        {
            ChangelogChangeKind.Feature => "feature",
            ChangelogChangeKind.Fix => "fix",
            _ => "improvement",
        };

        Label = change.Kind switch
        {
            ChangelogChangeKind.Feature => "NEU",
            ChangelogChangeKind.Fix => "BEHOBEN",
            _ => "GEÄNDERT",
        };

        Text = change.Text;
    }

    /// <summary>Machine readable kind, used by the popup to tint the badge.</summary>
    public string Kind { get; }

    /// <summary>Short badge text: NEU / GEÄNDERT / BEHOBEN.</summary>
    public string Label { get; }

    public string Text { get; }
}

/// <summary>One released version with all of its bullet points.</summary>
public sealed class ChangelogReleaseViewModel
{
    public ChangelogReleaseViewModel(ChangelogRelease release)
    {
        Version = release.Version;
        Heading = string.IsNullOrWhiteSpace(release.Title)
            ? $"Version {release.Version}"
            : $"Version {release.Version} – {release.Title}";
        Date = FormatDate(release.Date);
        Changes = [.. release.Changes.Select(change => new ChangelogChangeViewModel(change))];
    }

    public string Version { get; }

    public string Heading { get; }

    public string Date { get; }

    public IReadOnlyList<ChangelogChangeViewModel> Changes { get; }

    private static string FormatDate(string date)
        => DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : date;
}
