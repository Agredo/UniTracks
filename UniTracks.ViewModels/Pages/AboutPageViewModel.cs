using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UniTracks.ViewModels.Pages;

public partial class AboutPageViewModel : ObservableObject
{
    public string AppName { get; } = "UniTracks";
    public string AppVersion { get; }

    /// <summary>
    /// Kurze Erklärung, was Agredo Application ist.
    /// </summary>
    public string AgredoText { get; } =
        "Agredo Application ist das Entwicklerstudio hinter UniTracks. " +
        "Mit Fokus auf Datenschutz und Privatsphäre bauen wir Apps, die alle Daten " +
        "lokal auf deinem Gerät halten – ohne Cloud, ohne Tracking, ohne versteckte Analyse.";

    /// <summary>
    /// Bremen mit Liebe.
    /// </summary>
    public string BremenText { get; } =
        "UniTracks wurde in Bremen entwickelt – mit viel Liebe. ❤️";

    public ObservableCollection<LibraryInfo> Libraries { get; }

    public AboutPageViewModel(string appVersion)
    {
        AppVersion = appVersion;
        Libraries = new ObservableCollection<LibraryInfo>(BuildLibraries());
    }

    private static IEnumerable<LibraryInfo> BuildLibraries()
    {
        yield return new LibraryInfo
        {
            Name = ".NET MAUI (Microsoft.Maui.Controls)",
            Version = "11.0.0-preview.7.26406.9",
            License = "MIT",
            GitHubUrl = "https://github.com/dotnet/maui",
        };
        yield return new LibraryInfo
        {
            Name = "CommunityToolkit.Maui",
            Version = "15.0.1",
            License = "MIT",
            GitHubUrl = "https://github.com/CommunityToolkit/Maui",
        };
        yield return new LibraryInfo
        {
            Name = "CommunityToolkit.Mvvm",
            Version = "8.4.2",
            License = "MIT",
            GitHubUrl = "https://github.com/CommunityToolkit/dotnet",
        };
        yield return new LibraryInfo
        {
            Name = "Mapsui.Maui",
            Version = "5.1.0",
            License = "MIT",
            GitHubUrl = "https://github.com/Mapsui/Mapsui",
        };
        yield return new LibraryInfo
        {
            Name = "SkiaSharp.Views.Maui.Controls",
            Version = "3.119.2",
            License = "MIT",
            GitHubUrl = "https://github.com/mono/SkiaSharp",
        };
        yield return new LibraryInfo
        {
            Name = "LiteDB",
            Version = "6.0.0-prerelease.81",
            License = "MIT",
            GitHubUrl = "https://github.com/litedb-org/LiteDB",
        };
        yield return new LibraryInfo
        {
            Name = "Microsoft.EntityFrameworkCore.Sqlite",
            Version = "11.0.0-preview.7.26381.103",
            License = "MIT",
            GitHubUrl = "https://github.com/dotnet/efcore",
        };
        yield return new LibraryInfo
        {
            Name = "Microsoft.EntityFrameworkCore.Design",
            Version = "11.0.0-preview.7.26381.103",
            License = "MIT",
            GitHubUrl = "https://github.com/dotnet/efcore",
        };
        yield return new LibraryInfo
        {
            Name = "Microsoft.EntityFrameworkCore.Tools",
            Version = "11.0.0-preview.7.26381.103",
            License = "MIT",
            GitHubUrl = "https://github.com/dotnet/efcore",
        };
        yield return new LibraryInfo
        {
            Name = "SQLitePCLRaw.bundle_e_sqlite3",
            Version = "2.1.13",
            License = "Apache-2.0",
            GitHubUrl = "https://github.com/ericsink/SQLitePCL.raw",
        };
        yield return new LibraryInfo
        {
            Name = "Microsoft.Extensions.Logging.Debug",
            Version = "11.0.0-preview.7.26381.103",
            License = "MIT",
            GitHubUrl = "https://github.com/dotnet/runtime",
        };
        yield return new LibraryInfo
        {
            Name = "GeoCoordinate.NetStandard1",
            Version = "1.0.1",
            License = "MS-PL",
            GitHubUrl = "https://github.com/AeonLucid/GeoCoordinate.NetStandard1",
        };
        yield return new LibraryInfo
        {
            Name = "System.Net.Http",
            Version = "4.3.4",
            License = "MIT",
            GitHubUrl = "https://github.com/dotnet/runtime",
        };
        yield return new LibraryInfo
        {
            Name = "System.Text.RegularExpressions",
            Version = "4.3.1",
            License = "MIT",
            GitHubUrl = "https://github.com/dotnet/runtime",
        };
        yield return new LibraryInfo
        {
            Name = "AgredoApplication.MVVM.Services",
            Version = "1.2.0",
            License = "Proprietär (Agredo Application)",
        };
        yield return new LibraryInfo
        {
            Name = "AgredoApplication.MVVM.Services.Maui",
            Version = "1.2.0",
            License = "Proprietär (Agredo Application)",
        };
    }
}
