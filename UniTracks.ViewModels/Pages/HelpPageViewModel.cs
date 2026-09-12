using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Inhalte der Hilfe-Seite: Navigation, Münzsystem, die beiden Spiele und die Errungenschaften.
/// Alle Werte sind statisch aus dem Spiel-/Ökonomie-Code abgeleitet, damit die Hilfe exakt
/// zum Verhalten der App passt.
/// </summary>
public partial class HelpPageViewModel : ObservableObject
{
    public string AppName { get; } = "UniTracks";

    public string AppVersion { get; }

    public ObservableCollection<HelpSection> Sections { get; } = new();

    public HelpPageViewModel(string appVersion)
    {
        AppVersion = appVersion;
        Sections.Add(BuildNavigation());
        Sections.Add(BuildCoins());
        Sections.Add(BuildCozyCity());
        Sections.Add(BuildTrailDefense());
        Sections.Add(BuildAchievements());
    }

    private static HelpSection BuildNavigation() => new()
    {
        Title = "Navigation",
        Icon = "🧭",
        Items =
        {
            new HelpItem
            {
                Title = "Die fünf Bereiche",
                Icon = "🛤️",
                Body = "• Trips (Karte): Deine aufgezeichneten Trips, Karte und Verlauf.\n" +
                       "• Record (Aufnahme): Zeichnet einen neuen Trip per GPS auf.\n" +
                       "• User (Person): Dein Profil mit Level, Streak und Gesamtkilometern — sowie Statistiken, Datenbank teilen, Über und Feedback.\n" +
                       "• Erfolge (Pokal): Deine freigeschalteten Errungenschaften.\n" +
                       "• Spiel (Gamepad): Dein Münzkonto und die Minispiele.",
            },
            new HelpItem
            {
                Title = "Einen Trip aufzeichnen",
                Icon = "▶️",
                Body = "Im Tab »Record« eine Sportart wählen und den Aufnahme-Knopf drücken. Beim ersten Mal fragt die App nach Standortberechtigungen. Während der Aufnahme läuft ein Timer; mit dem Stopp-Knopf beendest du den Trip, der danach im Tab »Trips« erscheint.",
            },
        },
    };

    private static HelpSection BuildCoins() => new()
    {
        Title = "Münzen",
        Icon = "🪙",
        Items =
        {
            new HelpItem
            {
                Title = "So funktioniert das Münzsystem",
                Icon = "💡",
                Body = "Münzen werden nie gespeichert — der Saldo wird bei jedem Öffnen des Spiel-Bereichs automatisch aus deinen echten Trips, Errungenschaften und Level neu berechnet. Neue Aktivität erhöht ihn sofort; Ausgaben werden abgezogen. Cozy City und Trail Defense teilen sich ein einziges Konto: Was du im einen Spiel ausgibst, fehlt sofort im anderen. So kann das Konto nie auseinanderlaufen.",
            },
            new HelpItem
            {
                Title = "Wo du Münzen bekommst",
                Icon = "💰",
                Body = "• Startbonus: 500 🪙 für alle.\n" +
                       "• Pro Trip: 5 🪙.\n" +
                       "• Pro Kilometer: 10 🪙 × km × Anstrengungsfaktor.\n" +
                       "• Pro Level-Up: wachsend (100 × (2+3+…+Level)).\n" +
                       "• Pro Errungenschaft: 25 🪙.",
            },
            new HelpItem
            {
                Title = "Anstrengungsfaktor pro Kilometer",
                Icon = "⚖️",
                Body = "• Laufen / Wandern: ×1,0 (Referenz)\n" +
                       "• Wassersport: ×1,2\n" +
                       "• Wintersport: ×0,8\n" +
                       "• Skaten: ×0,7\n" +
                       "• Indoor / Ball- & Kampfsport: ×0,5\n" +
                       "• Radfahren: ×0,3\n" +
                       "• E-Bike: ×0,1\n\n" +
                       "Beispiel: 10 km Lauf ≈ 100 🪙, 10 km Rad ≈ 30 🪙 — jeweils plus 5 🪙 pro Trip.",
            },
            new HelpItem
            {
                Title = "Wann du Münzen bekommst",
                Icon = "⏱️",
                Body = "Jeder abgeschlossene, gültige Trip, jede neu freigeschaltete Errungenschaft und jeder Levelaufstieg schlägt sich sofort im Saldo nieder — ganz automatisch, ohne manuelles Klicken.",
            },
            new HelpItem
            {
                Title = "Wie du Münzen einlöst",
                Icon = "🔄",
                Body = "• Cozy City: Gebäude bauen (15–500 🪙), Stadt erweitern (300 / 800 / 1500 🪙); Abriss erstattet 50 %.\n" +
                       "• Trail Defense: Türme freischalten (150–1000 🪙) und Energie im Lauf mit Münzen kaufen (2 🪙 pro ⚡, Paket 25 ⚡ = 50 🪙).",
            },
        },
    };

    private static HelpSection BuildCozyCity() => new()
    {
        Title = "Cozy City",
        Icon = "🏙️",
        Items =
        {
            new HelpItem
            {
                Title = "Was ist das?",
                Icon = "🌇",
                Body = "Ein gemütlicher Städtebau: Baue dir aus deinen Trips und Erfolgen eine kleine eigene Stadt. Die Stadt ist ein Raster, das du mit Gebäuden füllst.",
            },
            new HelpItem
            {
                Title = "Raster erweitern",
                Icon = "📐",
                Body = "Start: 6×6. Du kannst die Stadt erweitern — 8×8 (Level 2, 300 🪙), 10×10 (Level 4, 800 🪙), 12×12 (Level 7, 1500 🪙).",
            },
            new HelpItem
            {
                Title = "Gebäude & Preise",
                Icon = "🏗️",
                Body = "• Blumenbeet 15, Baum 20, Nadelbaum 20, Brunnen 60, Haus 80\n" +
                       "• Spielplatz 90 (Level 2), Café 100 (Level 2), Laden 120 (Level 3), Villa 150 (Level 3)\n" +
                       "• Schule 200 (Level 4), Krankenhaus 300 (Level 5)\n\n" +
                       "Prestige-Gebäude (durch Errungenschaften):\n" +
                       "• Goldene Statue 500 (100 km gesamt)\n" +
                       "• Gipfel-Kreuz 400 (Gipfelstürmer)\n" +
                       "• Marathon-Torbogen 400 (Marathon)",
            },
            new HelpItem
            {
                Title = "Abriss",
                Icon = "🧹",
                Body = "Ein Gebäude abreißen gibt dir die Hälfte (50 %) der Baukosten zurück — nützlich, um deine Stadt umzubauen.",
            },
        },
    };

    private static HelpSection BuildTrailDefense() => new()
    {
        Title = "Trail Defense",
        Icon = "🗼",
        Items =
        {
            new HelpItem
            {
                Title = "Was ist das?",
                Icon = "🛡️",
                Body = "Ein Turmverteidigungs-Spiel: Verteidige deinen Trail gegen herannahende Mückenschwärme. Du platzierst Türme, die die Gegner automatisch angreifen.",
            },
            new HelpItem
            {
                Title = "Energie (statt Münzen)",
                Icon = "⚡",
                Body = "Zum Platzieren von Türmen brauchst du Energie. Jede Welle gibt einen kleinen sportbasierten Bonus zurück: Basis 20, plus 10 je Level über 1, plus 15 je Errungenschaft, plus 5 je 10 Lebens-Kilometer — maximal 150. Start-Energie: 60. Ist sie aufgebraucht, kaufst du mit Münzen Energie nach (2 🪙 pro ⚡).",
            },
            new HelpItem
            {
                Title = "Türme freischalten",
                Icon = "🔓",
                Body = "• Mückenspray: kostenlos\n" +
                       "• Duftkerze 150 (Level 2)\n" +
                       "• Elektro-Falle 400 (Level 3)\n" +
                       "• Frosch 650 (Level 4)\n" +
                       "• Gecko 1000 (Level 5 + 100-km-Errungenschaft)",
            },
            new HelpItem
            {
                Title = "Gegner & Wellen",
                Icon = "🐝",
                Body = "Gegner: Mücke, Schnake, Wespe und die Hornisse (Boss alle 5 Wellen). Die Wellen sind endlos und werden pro Welle um 18 % Leben stärker.",
            },
            new HelpItem
            {
                Title = "Karten (Welten)",
                Icon = "🗺️",
                Body = "• Waldwiese: leicht und offen.\n" +
                       "• Park-Promenade: Level 2.\n" +
                       "• Seeufer: 3-Tage-Streak.\n" +
                       "• Altstadt-Gasse: 50 km.\n" +
                       "• Streifzug: 100 km + 7-Tage-Streak.",
            },
            new HelpItem
            {
                Title = "Leben & Rekord",
                Icon = "❤️",
                Body = "Leben sinken, wenn ein Gegner das Ende des Trails erreicht. Bei 0 Leben ist der Lauf verloren. Nur Wellen, die du ganz sauber (ohne Leck) schaffst, zählen für deinen Rekord.",
            },
        },
    };

    private static HelpSection BuildAchievements() => new()
    {
        Title = "Errungenschaften",
        Icon = "🏆",
        Items =
        {
            new HelpItem
            {
                Title = "Alle Errungenschaften",
                Icon = "🎖️",
                Body = "• Erster Trip: 1 Trip\n" +
                       "• 10 Trips / 25 Trips\n" +
                       "• 10 km / 50 km / 100 km gesamt\n" +
                       "• Langer Trip: ein Trip über 10 km\n" +
                       "• Marathon-Bereit: ein Trip über 42,2 km\n" +
                       "• Gipfelstürmer: über 1000 m Höhe\n" +
                       "• 3-Tage-, 7-Tage- und 30-Tage-Streak\n\n" +
                       "Jede Errungenschaft bringt 25 🪙 und erhöht den Energie-Bonus in Trail Defense.",
            },
        },
    };
}
