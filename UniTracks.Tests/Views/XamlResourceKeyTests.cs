using System.Xml.Linq;

namespace UniTracks.Tests.Views;

/// <summary>
/// A <c>ResourceDictionary</c> throws <c>ArgumentException</c> when two entries share a key, and an
/// implicit <c>Style</c> (one without <c>x:Key</c>) is keyed by its <c>TargetType</c>. Because the
/// merged dictionaries are built in <c>App.InitializeComponent</c>, such a duplicate is not a
/// compile warning but a hard crash before the first page appears — the app simply quits on launch.
/// <para>
/// That is what happened when a second <c>Style TargetType="Switch"</c> was added next to the
/// existing one. Nothing in the build or the view tests noticed, so this test keeps the two style
/// files free of duplicate keys.
/// </para>
/// </summary>
public sealed class XamlResourceKeyTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2009/xaml";

    [Fact]
    public void MergedStyleDictionaries_DoNotDefineTheSameKeyTwice()
    {
        var offenders = new List<string>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in StyleFiles())
        {
            foreach (var resource in Resources(file))
            {
                string? key = resource.Attribute(Xaml + "Key")?.Value
                    ?? resource.Attribute("TargetType")?.Value;

                if (key is null)
                {
                    continue;
                }

                string origin = $"{Relative(file)}: {resource.Name.LocalName} '{key}'";

                if (seen.TryGetValue(key, out string? first))
                {
                    offenders.Add($"{origin} doppelt (bereits in {first})");
                }
                else
                {
                    seen[key] = origin;
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    /// <summary>Top level entries of a resource dictionary, ignoring keys nested inside a style.</summary>
    private static IEnumerable<XElement> Resources(string file)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(file);
        }
        catch (Exception)
        {
            return [];
        }

        return document.Descendants()
            .Where(element => element.Parent?.Name.LocalName == "ResourceDictionary")
            .ToList();
    }

    /// <summary>The dictionaries merged into the application resources at startup.</summary>
    private static IEnumerable<string> StyleFiles() =>
        Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot(), "UniTracks.Maui", "Resources", "Styles"),
            "*.xaml",
            SearchOption.TopDirectoryOnly);

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
