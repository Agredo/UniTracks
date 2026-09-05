# UniTracks

Cross-Plattform Sport-Tracking-App mit **.NET 11** und **.NET MAUI** — Fokus auf Datensicherheit und Privatsphäre. Alle Daten bleiben lokal auf dem Gerät.

![Platform](https://img.shields.io/badge/platform-Android%20%7C%20iOS%20%7C%20MacCatalyst%20%7C%20Windows-4DE790)
![.NET](https://img.shields.io/badge/.NET-11-2B0B98)

## Features

- 📍 **GPS-Tracking** — Trips aufzeichnen mit Live-Standortdaten (Geschwindigkeit, Höhe, Genauigkeit)
- 🏷️ **85+ Trip-Typen** — von Run über Gassi gehen 🐕 bis Kayak, aus einem JSON-Seed-Katalog
- 🗺️ **Geschwindigkeits-Gradient-Route** — die Strecke färbt sich je nach Tempo: Lavender (langsam) → Mint (mittel) → Rot (schnell)
- 🏆 **Gamification** — Erfolge-Tab mit Level, XP, Streaks und Badges (Erster Trip, 10/25 Trips, Distanz-Meilensteine u. v. m.)
- 🎮 **Spiel-Tab** — Erfolge werden zur Währung: Coins verdienen durch Aktivität (10 🪙/km + 5 🪙/Trip + 25 🪙/Erfolg + 500 🪙 Startguthaben)
- 🏙️ **Cozy City Builder** — isometrische Stadt bauen, gerendert mit SkiaSharp: 11 Gebäude, Pan/Pinch-Zoom, Ghost-Preview, Drop-in-Animationen, Coin-Sparkles, Wolken, Vögel und Tag/Nacht-Zyklus
- 🌑 **Dark 2026 Sports UI** — modernes dunkles Design mit Mint-Akzenten (#4DE790)
- 📊 **Trip-Statistiken** — Distanz, Dauer, Ø- und Max-Geschwindigkeit pro Trip
- 👤 **Lokale Profile** — Nutzer-Verwaltung komplett offline
- 💾 **Datenbank-Export** — Trips als Datei teilen
- 🪟 **Adaptive UI** — 1-Spalten-Liste auf Mobile, 2 Spalten auf breiten Fenstern

Weitere geplante Features siehe [FEATURE_IDEAS.md](FEATURE_IDEAS.md) und den Citybuilder-Plan in [GAME_CITYBUILDER_PLAN.md](GAME_CITYBUILDER_PLAN.md).

## Screenshots

iOS (iPhone)

![Trips](docs/screenshots/ios-trips.jpeg)

![Record Trip](docs/screenshots/ios-record-trip.jpeg)

![Profil](docs/screenshots/ios-profile.jpeg)

Android

![image](https://github.com/Agredo/UniTracks/assets/16531090/9b12c5f8-271c-46d0-b99a-4c6a57776576)

Windows

![Trips](docs/screenshots/windows-trips.png)

![Record](docs/screenshots/windows-record.png)

![Profil](docs/screenshots/windows-profile.png)

![Erfolge](docs/screenshots/windows-achievements.png)

![Spiel-Tab](docs/screenshots/windows-game-tab.png)

![Cozy City Builder](docs/screenshots/windows-city-builder.png)

## Architektur

Saubere Trennung in einzelne Projekte (Nexile-Stil), MVVM mit **CommunityToolkit.Mvvm** Source-Generatoren:

```
UniTracks.Maui            → App-Head (Composition Root, MauiProgram, Styles, Plattform-Code)
UniTracks.Maui.Views      → Pages, Tabs, Controls (XAML), Custom Controls mit [BindableProperty]
UniTracks.Maui.Services   → Plattform-Services (GPS-Listener, Dispatcher)
UniTracks.ViewModels      → ViewModels ([ObservableProperty], [RelayCommand])
UniTracks.Services        → App-Logik (Tracking, Data-Services, Gamification, Game-Services)
UniTracks.Games           → Spiele-Logik, dependency-frei (CityBuilder-Engine, Coin-Wirtschaft, Spiele-Katalog)
UniTracks.Data            → Persistenz: Entity Framework Core (SQLite) + LiteDB
UniTracks.Models          → Domänen-Modelle (Trip, Location, User, Weather)
UniTracks.Core            → Basis-Abstraktionen
UniTracks.Common          → Geteilte Konstanten/Utilities
```

Zentrale Versionsverwaltung aller Pakete in `Directory.Build.props` / `projects.props`.

## Tech-Stack

| Bereich | Technologie |
|---|---|
| Framework | .NET 11 / .NET MAUI 11 (Preview) |
| MVVM | CommunityToolkit.Mvvm 8.4 + AgredoApplication.MVVM.Services |
| UI-Toolkit | CommunityToolkit.Maui 15 (Popups, Behaviors, `[BindableProperty]`-Generator) |
| Karten | Mapsui.Maui 5.1 (SkiaSharp, OpenStreetMap-Tiles) |
| Game-Rendering | SkiaSharp 3.119 (prozedurale Vektor-Sprites, isometrische Karte) |
| Datenbank | EF Core 11 (SQLite) + LiteDB |
| Plattformen | Android 24+, iOS 16+, MacCatalyst 15+, Windows 10.0.17763+ |

## Build & Run

Voraussetzung: .NET 11 SDK mit MAUI-Workload (`dotnet workload install maui`).

```bash
# Windows
dotnet build UniTracks.Maui/UniTracks.Maui.csproj -f net11.0-windows10.0.26100.0 -t:Run

# Android
dotnet build UniTracks.Maui/UniTracks.Maui.csproj -f net11.0-android -t:Run

# iOS / MacCatalyst (macOS)
dotnet build UniTracks.Maui/UniTracks.Maui.csproj -f net11.0-ios -t:Run
```

> ⚠️ Klassenbibliotheken sind `net11.0`-only — das `-f`-Flag nur auf dem App-Head (`UniTracks.Maui`) verwenden, nicht auf der Solution.

## Datenschutz

UniTracks speichert **alle Daten ausschließlich lokal** auf dem Gerät. Keine Cloud, kein Tracking, keine Analyse-Drittanbieter. Karten-Tiles werden von OpenStreetMap geladen.
