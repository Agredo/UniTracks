# UniTracks

Cross-Plattform Sport-Tracking-App mit **.NET 11** und **.NET MAUI** — Fokus auf Datensicherheit und Privatsphäre. Alle Daten bleiben lokal auf dem Gerät.

![Platform](https://img.shields.io/badge/platform-Android%20%7C%20iOS%20%7C%20MacCatalyst%20%7C%20Windows-4DE790)
![.NET](https://img.shields.io/badge/.NET-11-2B0B98)

## Features

- 📍 **GPS-Tracking** — Trips aufzeichnen mit Live-Standortdaten (Geschwindigkeit, Höhe, Genauigkeit)
- 🗺️ **Geschwindigkeits-Gradient-Route** — die Strecke färbt sich je nach Tempo: Lavender (langsam) → Mint (mittel) → Rot (schnell)
- 🌑 **Dark 2026 Sports UI** — modernes dunkles Design mit Mint-Akzenten (#4DE790)
- 📊 **Trip-Statistiken** — Distanz, Dauer, Ø- und Max-Geschwindigkeit pro Trip
- 👤 **Lokale Profile** — Nutzer-Verwaltung komplett offline
- 💾 **Datenbank-Export** — Trips als Datei teilen
- 🪟 **Adaptive UI** — 1-Spalten-Liste auf Mobile, 2 Spalten auf breiten Fenstern

Weitere geplante Features siehe [FEATURE_IDEAS.md](FEATURE_IDEAS.md).

## Screenshots

iOS (iPhone)

![image](https://github.com/Agredo/UniTracks/assets/16531090/3182082f-64b7-46fb-a2c3-03f0b898856b)

Android

![image](https://github.com/Agredo/UniTracks/assets/16531090/9b12c5f8-271c-46d0-b99a-4c6a57776576)

Windows

![image](https://github.com/Agredo/UniTracks/assets/16531090/5598f7ac-5d63-4529-bbc8-d8a626d727ff)

## Architektur

Saubere Trennung in einzelne Projekte (Nexile-Stil), MVVM mit **CommunityToolkit.Mvvm** Source-Generatoren:

```
UniTracks.Maui            → App-Head (Composition Root, MauiProgram, Styles, Plattform-Code)
UniTracks.Maui.Views      → Pages, Tabs, Controls (XAML), Custom Controls mit [BindableProperty]
UniTracks.Maui.Services   → Plattform-Services (GPS-Listener, Dispatcher)
UniTracks.ViewModels      → ViewModels ([ObservableProperty], [RelayCommand])
UniTracks.Services        → App-Logik (Tracking, Data-Services)
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
