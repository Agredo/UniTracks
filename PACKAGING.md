# UniTracks – Paketierung (APK, MSIX, IPA)

Alles ist vorbereitet, um Installationspakete zu bauen. Vorbild sind die Apps
**Nexile** und **Finanzio** (gleiche Keystore-/Zertifikats-Konventionen).

## App-Icon & Splash

- Design: gestrichelte Route mit Startpunkt und Ziel-Pin (Grün `#22C55E` / `#4ADE80` / `#BBF7D0`)
  auf dunklem Hintergrund `#020617` – im Stil von Finanzio.
- Dateien: `UniTracks.Maui/Resources/AppIcon/appicon.svg` (Hintergrund),
  `appiconfg.svg` (Vordergrund), `Resources/Splash/splash.svg`.
- Icon- und Splash-Hintergrundfarbe in der csproj: `#020617`.

## Identitäten

| Plattform | Wert |
|---|---|
| ApplicationId | `com.agredoapplication.unitracks` |
| Windows Identity | `21006AgredoApplication.UniTracks` (wie bei Finanzio/Nexile aus dem Partner Center) |
| Publisher | `CN=B267D9E1-E6F1-4B60-B1C1-BFA75BF2BF2B` |

> ⚠️ Prüfe im Partner Center, ob der Paketname `UniTracks` reserviert ist –
> sonst `Package.appxmanifest` (Identity `Name`) und ggf. `PublisherDisplayName` anpassen.

## Android APK

- Signiert mit `UniTracks.Maui/unitracks.keystore` (Alias `unitracks`,
  Passwort wie die Nexile-/Finanzio-Keystores). **Der Keystore liegt nur lokal
  und ist per `.gitignore` ausgeschlossen – unbedingt separat sichern!**
- Signierung ist in `UniTracks.Maui.csproj` unter `Release|net11.0-android` konfiguriert.
- Bauen:
  ```bat
  build-unitracks.bat
  ```
  oder manuell:
  ```bat
  dotnet publish UniTracks.Maui\UniTracks.Maui.csproj -f net11.0-android -c Release -p:AndroidPackageFormat=apk
  ```
- Ergebnis: `dist\UniTracks-<Version>-android.apk`

## Windows MSIX

- Signaturzertifikat: Thumbprint `727E15C7BE0E2740F464383F1D81FE6314C1F357`
  (Publisher-CN aus dem Partner Center, liegt in `CurrentUser\My`).
- Publish-Profile: `UniTracks.Maui/Properties/PublishProfiles/MSIX-win-x64.pubxml`
  und `MSIX-win-arm64.pubxml` (`WindowsPackageType=MSIX`).
- Normaler F5-/Debug-Start bleibt unverpackt (`WindowsPackageType=None` in der csproj).
- Bauen (x64 **und** arm64):
  ```bat
  build-unitracks.bat
  ```
- Ergebnis: `dist\UniTracks-<Version>-x64.msix` und `dist\UniTracks-<Version>-arm64.msix`

### Warum das Build-Skript vor jedem MSIX-Publish aufräumt

Die referenzierten Klassenbibliotheken (`UniTracks.Services`, `UniTracks.Data`, …)
teilen einen generischen Windows-TFM-Ausgabepfad ohne RID. Ohne das Löschen von
`bin/obj\Release\net11.0-windows10.0.26100.0` würde beim zweiten Publish die
Architektur des ersten Pakets wiederverwendet (stille Fehlpaketierung).
Deshalb löscht `build-unitracks.bat` diese Ordner vor jedem Publish und setzt
`-p:PlatformTarget` als globalen Schalter, damit **alle** Projekte für die
Zielarchitektur kompilieren.

## iOS IPA (nur auf einem Mac)

1. Im Apple Developer Portal für `com.agredoapplication.unitracks`:
   - **Distribution-Zertifikat** (Apple Distribution) erstellen,
   - **App-Store-Provisioning-Profil** erstellen,
   - beides auf dem Mac installieren (Xcode → Settings → Accounts).
2. In `UniTracks.Maui.csproj` den kommentierten iOS-Release-Block eintragen
   (`CodesignKey` + `CodesignProvision`, Kommentar entfernen).
3. Auf dem Mac:
   ```bash
   dotnet publish UniTracks.Maui/UniTracks.Maui.csproj -f net11.0-ios -c Release
   ```
   Mit `ArchiveOnBuild=true` entsteht das IPA direkt beim Build
   (`bin/Release/net11.0-ios/.../publish/`).

## Versionsnummern

- Android/iOS: `ApplicationDisplayVersion` / `ApplicationVersion` in der csproj.
- Windows: `Version` im `Package.appxmanifest` (aktuell `1.0.0.0`).
- Dateinamen der Artefakte: `VERSION` in `build-unitracks.bat`.
