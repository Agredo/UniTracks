namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Eine verwendete Bibliothek mit Name, Version, Lizenz und optionalem Link zum
/// GitHub-Repository. Wird im "Über"-Bereich angezeigt und beim Antippen im Browser geöffnet.
/// </summary>
public class LibraryInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string? GitHubUrl { get; set; }

    /// <summary>Ob ein GitHub-Link vorhanden und damit antippbar ist.</summary>
    public bool HasGitHubUrl => !string.IsNullOrWhiteSpace(GitHubUrl);
}
