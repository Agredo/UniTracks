# UniTracks — Feature-Ideen & Roadmap

Ideen für kommende Features, priorisiert nach Wert und Aufwand.
Legende: 🟢 kleiner Aufwand · 🟡 mittlerer Aufwand · 🔴 großer Aufwand

---

## 🏗️ Stabilität & Datenmanagement

| Feature | Aufwand | Beschreibung |
|---|---|---|
| Trip bearbeiten & löschen | 🟢 | Swipe-Actions auf Trip-Karten (Name ändern, löschen mit Bestätigung) |
| Datenbank-Import | 🟡 | Gegenstück zum vorhandenen "Datenbank teilen"-Export; Restore mit Validierung |
| Auto-Backup | 🟡 | Regelmäßiges automatisches Backup der SQLite-Datenbank |
| Aufnahme-Wiederherstellung | 🟡 | Ungespeicherte Aufnahme nach App-Absturz/Neustart wiederherstellen |

## 🏃 Kern-Sportfeatures

| Feature | Aufwand | Beschreibung |
|---|---|---|
| **Live-Tracking** | 🔴 | Karte im Record-Tab, die während der Aufnahme live mitzeichnet (aktuell nur Timer) — größter Sprung Richtung "echte Sport-App" |
| Live-Statistiken | 🟡 | Distanz, aktuelle Pace, Ø-Pace und Höhe während der Aufnahme anzeigen |
| **Auto-Pause** | 🟡 | Aufnahme pausiert automatisch bei Stillstand, startet bei Bewegung wieder |
| Pace statt nur km/h | 🟢 | min/km-Anzeige (Standard für Läufer) zusätzlich zu km/h |
| **Splits** | 🟡 | km-Rundenzeiten mit Vibration/Ansage pro Kilometer |
| Trip-Typen nutzen | 🟡 | Laufen / Radfahren / Wandern — Seeds existieren bereits im Data-Layer; Auswahl beim Start, Icons & Filter in der Liste |
| Höhenprofil & Charts | 🟡 | Geschwindigkeit/Höhe über Zeit als Diagramm auf der Trip-Übersicht (z.B. Microcharts) |
| GPX/FIT-Export | 🟡 | Export pro Trip für Strava/Garmin-Kompatibilität |

## 📊 Motivation & Soziales

| Feature | Aufwand | Beschreibung |
|---|---|---|
| **Dashboard** | 🔴 | Wochen-/Monatskilometer, Streak-Kalender, persönliche Rekorde auf der Startseite |
| Ziele | 🟡 | z.B. "50 km im Monat" mit Fortschrittsbalken |
| Achievements/Badges | 🟡 | Längster Trip, schnellster km, 100 km gesamt, 7-Tage-Streak … |
| Wetter pro Trip | 🟢 | Felder existieren bereits im Trip-Model — beim Speichern Wetter-API abfragen |
| Herzfrequenz | 🔴 | BLE-Herzfrequenz-Sensor anbinden (Feld existiert bereits im Trip-Model) |
| Foto pro Trip | 🟡 | Bilder an Trips hängen, in der Karte als Marker anzeigen |

## ⚙️ UX & Plattform

| Feature | Aufwand | Beschreibung |
|---|---|---|
| Sprachausgabe | 🟡 | Text-to-Speech: Stats-Ansage alle km ("Kilometer 5, Pace 5:30") |
| Dark/Light-Umschalter | 🟡 | Aktuell nur Dark hardcoded — AppThemeBinding für Light-Theme ergänzen |
| Onboarding | 🟡 | Erster-Start-Flow: Berechtigungen erklären, Profil anlegen, Tour |
| Windows-Polish | 🟢 | Fenster-Größe merken, Titlebar ans Dark-Theme anpassen |
| App-Icon & Splash | 🟢 | Icon/Splashscreen ans 2026-Design anpassen (Mint auf Schwarz-Grün) |
| Haptik | 🟢 | Haptic Feedback bei Start/Stop/Splits |

## 🚀 Empfohlene Reihenfolge

1. **Live-Tracking im Record-Tab** — Kern-Feature einer Sport-App
2. **Live-Statistiken + Auto-Pause** — macht die Aufnahme alltagstauglich
3. **Trip-Typen + Splits** — Seeds liegen bereit, hoher Nutzen
4. **Dashboard** — Motivation & Wiederkommen
5. **GPX-Export** — Ökosystem-Anschluss (Strava etc.)
