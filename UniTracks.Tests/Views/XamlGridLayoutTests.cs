using System.Xml.Linq;

namespace UniTracks.Tests.Views;

/// <summary>
/// MAUI clamps <c>Grid.Row</c>/<c>Grid.Column</c> to the last defined row/column
/// (<c>GridLayoutManager</c> falls back to a single implied star definition when the collection is
/// empty), so a Grid without enough <c>RowDefinitions</c>/<c>ColumnDefinitions</c> silently stacks
/// its children on top of each other instead of laying them out.
/// <para>
/// That is how the trip analysis page lost half of its statistic chips: the chip grid declared four
/// columns, four chips in row 1 and no row definitions, so the second row was drawn over the first.
/// </para>
/// </summary>
public sealed class XamlGridLayoutTests
{
    private static readonly XNamespace Maui = "http://schemas.microsoft.com/dotnet/2021/maui";

    [Fact]
    public void EveryGrid_DeclaresEnoughRowsAndColumnsForItsChildren()
    {
        var offenders = new List<string>();

        foreach (var file in XamlFiles())
        {
            XDocument document;
            try
            {
                document = XDocument.Load(file);
            }
            catch (Exception exception)
            {
                offenders.Add($"{Relative(file)}: nicht lesbar ({exception.Message})");
                continue;
            }

            foreach (var grid in document.Descendants(Maui + "Grid"))
            {
                int rows = DefinitionCount(grid.Attribute("RowDefinitions"));
                int columns = DefinitionCount(grid.Attribute("ColumnDefinitions"));

                int maxRow = MaxIndex(grid, "Grid.Row");
                int maxColumn = MaxIndex(grid, "Grid.Column");

                if (maxRow >= rows)
                {
                    offenders.Add($"{Relative(file)}: Grid.Row=\"{maxRow}\" ohne RowDefinitions ({rows} Zeilen)");
                }

                if (maxColumn >= columns)
                {
                    offenders.Add($"{Relative(file)}: Grid.Column=\"{maxColumn}\" ohne ColumnDefinitions ({columns} Spalten)");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    private static IEnumerable<string> XamlFiles() =>
        Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot(), "UniTracks.Maui.Views"),
            "*.xaml",
            SearchOption.AllDirectories);

    /// <summary>Counts the definitions in an attribute such as <c>"Auto,Auto"</c> or <c>"*,2*"</c>.</summary>
    private static int DefinitionCount(XAttribute? attribute) =>
        attribute is null
            ? 0
            : attribute.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    /// <summary>Largest index used by a direct child, or -1 when no child sets the attached property.</summary>
    private static int MaxIndex(XElement grid, string attribute)
    {
        int maximum = -1;
        foreach (var child in grid.Elements())
        {
            if (int.TryParse(child.Attribute(attribute)?.Value, out int index))
            {
                maximum = Math.Max(maximum, index);
            }
        }

        return maximum;
    }

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
