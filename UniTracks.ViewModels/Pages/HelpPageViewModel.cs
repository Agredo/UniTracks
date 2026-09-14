using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Inhalte der Hilfe-Seite: Navigation, der Sport-Teil (Aufzeichnen und Tripvergleich), Profil und
/// Einstellungen, das Münzsystem, die beiden Spiele und die Errungenschaften. Alle Werte sind
/// statisch aus dem Spiel-, Ökonomie- und Vergleichs-Code abgeleitet, damit die Hilfe exakt zum
/// Verhalten der App passt.
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
        Sections.Add(BuildTrips());
        Sections.Add(BuildProfileAndSettings());
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
                       "• User (Person): Dein Profil mit Level, Streak und Gesamtkilometern — dazu Statistiken, »Profil bearbeiten«, Einstellungen und Feedback.\n" +
                       "• Erfolge (Pokal): Deine freigeschalteten Errungenschaften.\n" +
                       "• Spiel (Gamepad): Dein Münzkonto und die Minispiele.",
            },
            new HelpItem
            {
                Title = "Wo die Nebenseiten liegen",
                Icon = "🧷",
                Body = "Statistik, Profil und Feedback erreichst du im Tab »User« über die Knöpfe unter deinem Profil. Hilfe & Anleitung, Über die App und »Was ist neu?« liegen in den Einstellungen im Abschnitt »App«.",
            },
        },
    };

    private static HelpSection BuildTrips() => new()
    {
        Title = "Trips & Vergleich",
        Icon = "🏃",
        Items =
        {
            new HelpItem
            {
                Title = "Einen Trip aufzeichnen",
                Icon = "▶️",
                Body = "Im Tab »Record« wählst du oben die Sportart: Der aktive Sport steht groß über der Steuerung, die meistgenutzten liegen als Chips darunter — der zuletzt genutzte rutscht nach vorne, die ganze Liste mit Suche steckt hinter »Alle«. Der große Knopf in der Mitte startet und pausiert die Aufnahme, der runde Knopf daneben beendet sie und ist nur aktiv, solange ein Trip offen ist. Die Farben zeigen den Zustand: grün bereit, rot während der Aufnahme, amber pausiert. Beim ersten Mal fragt die App nach Standortberechtigungen; der fertige Trip erscheint im Tab »Trips«. Die Sportart kannst du auch während der Aufnahme noch wechseln: Tippe dazu auf die große Sportart über der Steuerung — sie gilt sofort für den laufenden Trip und steht danach auch in der Trip-Liste.",
            },
            new HelpItem
            {
                Title = "Aufzeichnung vom Sperrbildschirm steuern",
                Icon = "🔒",
                Body = "Läuft ein Run, musst du das Handy nicht entsperren: Auf dem Sperrbildschirm steht eine Anzeige mit den Knöpfen Pausieren, Fortsetzen und Stoppen. Auf dem iPhone ist das die Live Activity — eine Karte mit laufender Uhr und dem Zustand »Aufnahme läuft« bzw. »Aufnahme pausiert«, die zusätzlich in der Dynamic Island erscheint. Wenn dein iPhone keine Live Activities anzeigt (Systemeinstellungen »UniTracks«), übernimmt eine stille Benachrichtigung mit denselben Knöpfen; auf Android liegen sie ebenfalls in der Benachrichtigung. In der App selbst verschwindet die Anzeige, damit sie nicht doppelt erscheint.",
            },
            new HelpItem
            {
                Title = "Laufrichtung auf der Strecke",
                Icon = "➡️",
                Body = "Auf der Strecke wandern animierte Pfeile vom Start bis zum Ziel, damit sofort klar ist, in welche Richtung gelaufen wurde. Das Ende markiert eine karierte Zielflagge.",
            },
            new HelpItem
            {
                Title = "Trip nachträglich bearbeiten",
                Icon = "✏️",
                Body = "In der Trip-Übersicht öffnet der Bearbeiten-Dialog Name, Sportart und Notiz zum Ändern. Wechselst du die Sportart, gilt der Trip ab sofort als dieser Lauftyp: Vorschläge für ähnliche Strecken, Vergleich und Bestzeiten richten sich danach neu aus, weil der Streckenabdruck mit der neuen Sportart neu berechnet wird.",
            },
            new HelpItem
            {
                Title = "Vergleichen starten",
                Icon = "🔀",
                Body = "Öffne einen Trip und tippe auf »Vergleichen«. Die Seite schlägt die anderen Trips in drei Gruppen vor:\n" +
                       "• »Gleiche Strecke« — exakt dieselbe Strecke; diese Läufe zählen zur Bestzeit.\n" +
                       "• »Ähnliche Strecken« — ganz oder teilweise dieselben Wege.\n" +
                       "• »Vergleichbarer Umfang« — andere Strecken mit ähnlicher Länge und Anstrengung, gedacht für den Leistungsvergleich.",
            },
            new HelpItem
            {
                Title = "Mehrere Läufe auf einmal vergleichen",
                Icon = "📊",
                Body = "Du musst dich nicht auf zwei Läufe beschränken: Tippe in einer der Listen mehrere Läufe an (bis zu acht) und starte mit »Auswahl vergleichen«. Der schnellste Lauf wird dabei automatisch die Basis, gegen die alle anderen gerechnet werden — so siehst du über Monate hinweg, wie du dich auf derselben Runde entwickelt hast.",
            },
            new HelpItem
            {
                Title = "Wie ähnliche Strecken gefunden werden",
                Icon = "🧭",
                Body = "UniTracks legt für jeden Trip einen Streckenabdruck an: Die Strecke wird stark vereinfacht, in grobe Rasterzellen übertragen und über ihren Schwerpunkt verortet. Bei einem Vorschlag vergleicht die App deshalb nicht jede GPS-Spur mit jeder, sondern erst die billigen Merkmale — gemeinsame Rasterzellen und Streckenlänge — und prüft nur die verbleibenden Kandidaten genau auf Form und Richtung. So steht der Vorschlag auch bei vielen Trips sofort da. Warum ein Trip in der Liste steht, sagt die App jeweils in einer Zeile dazu.",
            },
            new HelpItem
            {
                Title = "Was der Vergleich zeigt",
                Icon = "📈",
                Body = "• Die Strecken übereinander auf der Karte — jeder Lauf in eigener Farbe, die Färbung folgt dem Tempo.\n" +
                       "• Eine Rangliste, sortiert nach steigungsbereinigter Pace, mit Zeit, Pace und der Abweichung Δ zur Basis.\n" +
                       "• Kennzahlen und den Pace-Verlauf aller Läufe auf einer gemeinsamen Skala.\n" +
                       "• Kilometer-Splits für die gemeinsam gelaufenen Kilometer — dort siehst du je Kilometer, wer schneller war.",
            },
            new HelpItem
            {
                Title = "Lauftyp und Fairness",
                Icon = "⚖️",
                Body = "Verglichen wird zuerst nur innerhalb desselben Lauftyps: Laufen bleibt unter Laufen. Der Schalter »Alle Lauftypen einbeziehen« nimmt Walk und Trailrun dazu; Bestzeit und Trend bleiben trotzdem auf deinen eigenen Lauftyp bezogen. Wenn Länge, Höhenmeter oder Pausen den Vergleich unfair machen, sagt die App es dir oben im Ergebnis.",
            },
        },
    };

    private static HelpSection BuildProfileAndSettings() => new()
    {
        Title = "Profil & Einstellungen",
        Icon = "⚙️",
        Items =
        {
            new HelpItem
            {
                Title = "Profil bearbeiten",
                Icon = "👤",
                Body = "Im Tab »User« unter »Profil bearbeiten« änderst du Name, E-Mail, Größe und Gewicht. Jedes gespeicherte Gewicht bleibt als Eintrag im Verlauf erhalten, damit sich die Entwicklung nachvollziehen lässt.",
            },
            new HelpItem
            {
                Title = "Kartenstil",
                Icon = "🗺️",
                Body = "In den Einstellungen wählst du, wie die Karte aussieht: Standard (OpenStreetMap), Hell (Carto) oder Topografisch (OpenTopoMap). Unter der Auswahl steht, von wem die Kartendaten kommen.",
            },
            new HelpItem
            {
                Title = "Kompakte Trip-Karten",
                Icon = "📇",
                Body = "Unter »Trips« in den Einstellungen entscheidest du, wie viel die Trip-Liste je Eintrag zeigt. Die ausführliche Karte trägt Symbol und Farbe der Sportart, das Datum relativ (»Heute«, »Gestern«, »vor 3 Tagen«), die wichtigsten Werte und — sofern vorhanden — Wetter, Puls und Höhe. Kompakt schrumpft jeder Trip auf eine Zeile mit Strecke, Dauer und Tempo, damit deutlich mehr Trips aufs Display passen.",
            },
            new HelpItem
            {
                Title = "GPS-Glättung der Karte",
                Icon = "〰️",
                Body = "Der Schalter in den Einstellungen entscheidet, was die Karte zeichnet: aus zeigt sie die rohen GPS-Punkte inklusive Zickzack und Ausreißern, an die gefilterte und gemittelte Strecke. Distanzen und Statistiken werden immer aus der geglätteten Strecke berechnet — Rekorde und Vergleiche bleiben also stabil.",
            },
            new HelpItem
            {
                Title = "Standort im Hintergrund",
                Icon = "📍",
                Body = "Damit eine Aufzeichnung weiterläuft, wenn du die App weglegst, braucht UniTracks die Freigabe »Immer erlauben«. Android 11+ und iOS bieten sie nicht im normalen Dialog an, deshalb führt der Knopf in den Einstellungen notfalls auf die Systemseite der App.",
            },
            new HelpItem
            {
                Title = "Datenbank sichern, teilen, importieren, löschen",
                Icon = "💾",
                Body = "»In Dateien speichern« legt eine Kopie deiner Daten über den Systemdialog dorthin, wo du sie haben willst — in den Ordner Documents, auf die SD-Karte oder in einen Cloud-Ordner; von dort holst du sie mit der Dateien-App, auf den PC oder per Messenger weiter. »Datenbank teilen« schickt die Kopie direkt an eine andere App. Die laufende Datei bleibt in beiden Fällen unangetastet. Ein Import wird geprüft und erst beim nächsten Start der App übernommen, ebenso das Löschen aller Daten — deine jetzige Datenbank bleibt als Kopie im App-Ordner. Beende die App dafür im App-Umschalter vollständig und starte sie neu.",
            },
            new HelpItem
            {
                Title = "Hilfe, Über und Was ist neu",
                Icon = "ℹ️",
                Body = "Im Abschnitt »App« der Einstellungen findest du diese Anleitung, die Seite über die App und den Knopf »Was ist neu?«: Er zeigt dir die Versionshinweise zu dieser Version jederzeit erneut — nicht nur einmal nach einem Update.",
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
