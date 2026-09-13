using System.Text.RegularExpressions;

namespace UniTracks.Tests.Views;

/// <summary>
/// Jede Seite der App wird von genau einer anderen Seite aus geöffnet: Profil, Statistiken,
/// Einstellungen und Feedback vom Profil-Tab, Hilfe und Über die App aus den Einstellungen.
/// Nachdem „Profil bearbeiten“ und „Hilfe“ versehentlich doppelt auftauchten (einmal im Hero und
/// einmal darunter bzw. in beiden Seiten), hält dieser Test jeden dieser Befehle auf genau einem
/// Vorkommen.
/// </summary>
public sealed class PageNavigationDuplicationTests
{
    /// <summary>Befehle, die auf eine andere Seite führen und deshalb nur einmal vorkommen dürfen.</summary>
    private static readonly string[] NavigationCommands =
    [
        "OpenProfileCommand",
        "OpenSettingsCommand",
        "OpenStatisticsCommand",
        "OpenHelpCommand",
        "OpenAboutCommand",
        "OpenFeedbackCommand"
    ];

    [Fact]
    public void NavigationCommands_AreUsedOnExactlyOnePage()
    {
        var usage = NavigationCommands.ToDictionary(command => command, _ => new List<string>());

        foreach (var file in PageFiles())
        {
            string content = File.ReadAllText(file);

            foreach (var command in NavigationCommands)
            {
                int count = Regex.Matches(content, $@"\b{command}\b").Count;

                for (int i = 0; i < count; i++)
                {
                    usage[command].Add(Relative(file));
                }
            }
        }

        var offenders = usage
            .Where(entry => entry.Value.Count != 1)
            .Select(entry => $"{entry.Key} ({entry.Value.Count}x): {string.Join(", ", entry.Value)}")
            .ToList();

        Assert.True(offenders.Count == 0, $"Befehl nicht genau einmal verwendet: {string.Join(" | ", offenders)}");
    }

    /// <summary>Alle Seiten der App — die Tab-Seiten liegen mit unter <c>Pages</c>.</summary>
    private static IEnumerable<string> PageFiles() =>
        new[] { "UniTracks.Maui.Views", "UniTracks.Maui" }
            .Select(project => Path.Combine(RepositoryRoot(), project))
            .Where(Directory.Exists)
            .SelectMany(project => Directory.EnumerateFiles(project, "*.xaml", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniTracks.Maui.Views")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"Repository-Wurzel über {AppContext.BaseDirectory} nicht gefunden.");
    }

    private static string Relative(string file) =>
        Path.GetRelativePath(RepositoryRoot(), file).Replace('\\', '/');
}
