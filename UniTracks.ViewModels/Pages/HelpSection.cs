using System.Collections.ObjectModel;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Ein einzelner Hilfeeintrag innerhalb eines Abschnitts — Titel, optionales Icon
/// und ein mehrzeiliger Erklärungstext (Bullet-Punkte mit "\n").
/// </summary>
public class HelpItem
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Ein inhaltlicher Abschnitt der Hilfe-Seite (z. B. „Navigation“, „Münzen“,
/// „Cozy City“). Enthält eine Liste von <see cref="HelpItem"/>.
/// </summary>
public class HelpSection
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ObservableCollection<HelpItem> Items { get; } = new();
}
