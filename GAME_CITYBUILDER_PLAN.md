# 🏙️ Feature-Plan: Spiele-Tab + Cozy City Builder

> Arbeitsdokument für eine neue Session. Stand: 2026-09-05, `main` = `de2c6b4` (Gamification gemergt).
> Empfehlung: Feature-Branch `feature/games` von `main` erstellen.

## Ziel

Ein neuer **„Spiel"-Tab** mit einer erweiterbaren Spiele-Liste. Erstes Spiel: ein **Cozy City Builder**,
bei dem Spieler mit **Coins** (verdient durch Trips & Erfolge) Gebäude auf einer Stadt-Karte kaufen und platzieren.
Architektur bleibt offen für weitere Spiele (z. B. Tower Defense später).

## Architektur-Entscheidung (vom Nutzer bestätigt)

| Schicht | Projekt | Inhalt |
|---|---|---|
| Spiellogik (NEU) | **`UniTracks.Games`** | Reines C#, **keine** MAUI-/SkiaSharp-/EF-Abhängigkeiten. Domain-Modelle, BuildingCatalog, CityEngine, CoinEconomy, GameCatalog, Ports (Interfaces), Persistenz-Entitäten (`PlacedBuilding`). Testbar, engine-agnostisch. |
| Persistenz | `UniTracks.Data` | Referenziert `UniTracks.Games` zusätzlich (DbSet). LiteDB (iOS) ist generisch → kein Zusatzaufwand. |
| Adapter/Services | `UniTracks.Services` | Implementiert die Ports aus `UniTracks.Games` via `IRepository` (Coins aus Trip-Daten, Stadt laden/speichern). |
| ViewModels | `UniTracks.ViewModels` | `GameTabPageViewModel`, `CityBuilderPageViewModel`. |
| UI | `UniTracks.Maui.Views` | `GameTabPage` (Tab), `CityBuilderPage` (geroutete Seite), SkiaSharp-Map-Control. |

**Begründung:** Ein neues Projekt, kein Over-Engineering mit 3 Projekten. UI braucht ohnehin MAUI+SkiaSharp,
bleibt daher in `Maui.Views`. `UniTracks.Games` kann später auch von anderen Hosts/Tests genutzt werden.

## Währung & Wirtschaft

- **XP** (bestehend, Gamification) = Prestige/Level, **unverbrauchbar**.
- **Coins** (neu) = ausgebbare Währung. `Balance = Earned − Spent`
  - Earned (aus Trip-Daten berechnet, wie Gamification — keine Sync-Probleme):
    `10 Coins pro km + 5 Coins pro Trip + 25 Coins pro freigeschaltetem Erfolg`
  - Spent = Summe der Kosten aller platzierten Gebäude (abrissbedingte Rückerstattung: 50 %).
- Persistiert wird nur: `PlacedBuilding` (ID Guid, BuildingId string, X int, Y int, PlacedAt DateTimeOffset).
  Coins werden NICHT persistiert (immer berechnet) → kein Cheating-Drift, keine Migration von Balances.

## Gebäude-Katalog (Start, `BuildingCatalog` im Games-Projekt)

| Gebäude | Icon | Kosten |
|---|---|---|
| Blumenbeet | 🌷 | 15 |
| Baum / Nadelbaum | 🌳 / 🌲 | 20 |
| Brunnen | ⛲ | 60 |
| Haus | 🏠 | 80 |
| Spielplatz | 🛝 | 90 |
| Café | ☕ | 100 |
| Laden | 🏪 | 120 |
| Villa | 🏡 | 150 |
| Schule | 🏫 | 200 |
| Krankenhaus | 🏥 | 300 |

## Rendering: SkiaSharp (beeindruckende Karte)

- **Version:** `SkiaSharp.Views.Maui.Controls` **3.119.2** — explizite PackageReference in
  `UniTracks.Maui.Views.csproj`. Grund: SkiaSharp kommt heute transitiv über `Mapsui.Maui 5.1.0`
  (pinnt exakt 3.119.2). KEIN Upgrade auf 4.x, sonst Konflikt mit Mapsui.
  `Directory.Build.props`: `SkiaSharpVersion` Property ergänzen.
- Eigenes Control `CityMapView : SKCanvasView` in `UniTracks.Maui.Views/Controls/Game/`:
  - **Isometrische Kachel-Karte** (6×6 Start), Gras-Gradient, leichte Farbvariation pro Kachel.
  - Gebäude als **Vektor-Sprites** prozedural gezeichnet (Haus mit Dach, Baum, Brunnen mit Wasser...).
  - **Animationen** via Timer + `InvalidateSurface()`: ziehende Wolken, Wasserschimmern im Brunnen,
    Vögel, Bounce-In beim Platzieren, Coin-Sparkle.
  - **Tageszeit-Tint** (warm morgens/abends, dunkler nachts, anhand Gerätezeit).
  - **Pan/Zoom** (Pinch/Pan-Gesten), Tap→Kachel über inverse Isometrie-Transformation.
  - **Ghost-Preview** des ausgewählten Gebäudes auf der angetippten Kachel.

## UI-Flow

1. **Spiel-Tab** (`GameTabPage`): Coins-Balance im Header, Karten-Liste verfügbarer Spiele
   (aktuell nur „Cozy City" 🏙️; Platzhalter „Bald verfügbar" demonstriert Erweiterbarkeit).
   Tap → `Navigation.ShellNavigationTo("CityBuilderPage", ...)` (Muster wie `TripOverviewPage`).
2. **CityBuilderPage**: oben Coins + Zurück; Mitte `CityMapView`; unten horizontaler Shop
   (Chips mit Icon+Kosten, Auswahl wie Trip-Typ-Chips); Modus-Toggle „🧨 Abriss" (50 % Refund).
3. Platzieren: Gebäude im Shop wählen → Kachel tippen → wenn leer & genug Coins → platzieren
   (Bounce-Animation, Coins aktualisieren). Belegte Kachel ohne Abriss-Modus: kurzes Wackeln/Toast.

## DB / Migration

- **Keine neue Migration** (alles vor erstem Release): bestehende InitialCreate-Migration löschen
  und mit neuem `DbSet<PlacedBuilding>` neu generieren:
  `dotnet ef migrations add InitialCreate --project UniTracks.Data` (DesignTime-Factory existiert).
- Danach lokale DB löschen, damit sie neu erstellt wird:
  - unpackaged: `%LocalAppData%\Agredo Application\21006AgredoApplication.UniTracks\Data\sqliteDatabase.db`
  - packaged: `%LocalAppData%\Packages\com.companyname.unitracks.maui_9zz4h110yvjzm\LocalState\sqliteDatabase.db`

## Shell / DI / Routing

- `AppShell.xaml`: neuer Tab **„Spiel"** mit neuem Icon `gamepad.svg` in `Resources/Images/Icons/`
  (MauiImage erzeugt automatisch `gamepad.png`).
- `App.xaml.cs`: `Routing.RegisterRoute(nameof(CityBuilderPage), typeof(CityBuilderPage));`
- `MauiProgram.cs`: `RegisterPages()` um `GameTabPage`/`GameTabPageViewModel` und
  `CityBuilderPage`/`CityBuilderPageViewModel` (Transient); Game-Services als Singleton.

## Evaluierte Alternativen (verworfen)

- **SkiaSharp 4.152.0-rc.1** — verworfen: RC-Prerelease, und Mapsui 5.1.0 pinnt SkiaSharp 3.119.2
  (Major-Version-Sprung = Laufzeitrisiko auf der Kartenansicht).
- **DrawnUI for MAUI** (`DrawnUi.Maui.Core` 1.10.x-preview) — interessant, aber jetzt verworfen:
  ersetzt die gesamte XAML-UI durch gezeichnete Skia-Controls (Paradigmenwechsel), nur als Preview
  für .NET 10 verfügbar — für einen einzelnen Spiel-Screen Overkill. **Merken** für den Fall, dass
  der Citybuilder später Vollbild mit komplett gezeichnetem HUD werden soll.

## ⚠️ Bekannte Stolpersteine

- **MAUIG1001 SourceGen-Bug** (.NET 11 Preview): `GridItemsLayout`/`LinearItemsLayout` in XAML
  **immer mit explizitem `Orientation="..."`** schreiben, sonst kryptischer Parse-Fehler.
- Windows ARM64: Target `net11.0-windows10.0.26100.0`; Exe:
  `UniTracks.Maui\bin\Debug\net11.0-windows10.0.26100.0\win-arm64\UniTracks.Maui.exe`
- Build: `dotnet build UniTracks.Maui\UniTracks.Maui.csproj -f net11.0-windows10.0.26100.0`
- Prozess stoppen nur via `Stop-Process -Id <PID>`.
- iOS nutzt LiteDB (generisch), Rest EF Core + SQLite — Services nur gegen `IRepository` bauen.

## Umsetzungs-Reihenfolge (Vorschlag)

1. `UniTracks.Games` Projekt + Solution-Eintrag, Domain-Modelle + Catalog + Engine + Ports
2. `UniTracks.Data`: Referenz + DbSet + InitialCreate neu generieren + lokale DB löschen
3. `UniTracks.Services`: CityBuilderService/CoinService (Adapter)
4. ViewModels + GameTabPage (Liste) + CityBuilderPage (erst einfach: Grid-Buttons statt Skia)
5. `CityMapView` SkiaSharp-Rendering + Animationen + Gesten (der „Wow"-Teil)
6. Shell-Tab, DI, Route, Icon → Build → App starten → visuelle Iteration
7. Commit/Push/Merge auf `main`
