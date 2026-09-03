@echo off
setlocal enabledelayedexpansion

set VERSION=1.0.0
set PROJECT=UniTracks.Maui\UniTracks.Maui.csproj
set ANDROID_TFM=net11.0-android
set WINDOWS_TFM=net11.0-windows10.0.26100.0
set DIST=dist

echo.
echo ========================================
echo  UniTracks Build v%VERSION%
echo ========================================
echo.

REM ---------------------------------------------------------------------------
REM JDK automatisch finden (Microsoft OpenJDK), falls JAVA_HOME ungueltig ist.
REM ---------------------------------------------------------------------------
set JAVA_ARG=
if exist "%JAVA_HOME%\bin\java.exe" goto jdk_ok
for /d %%j in ("C:\Program Files\Microsoft\jdk-*") do (
    if exist "%%j\bin\java.exe" (
        echo  [INFO] JDK: %%j
        set JAVA_ARG=-p:JavaSdkDirectory=%%j
        goto jdk_ok
    )
)
:jdk_ok

if not exist %DIST% mkdir %DIST%

REM ---------------------------------------------------------------------------
REM [1/3] Android APK (signiert, unitracks.keystore)
REM   Signierung ist in der csproj unter Release|net11.0-android konfiguriert.
REM ---------------------------------------------------------------------------
echo [1/3] Building Android APK...
dotnet publish %PROJECT% -f %ANDROID_TFM% -c Release -p:AndroidPackageFormat=apk %JAVA_ARG%
if errorlevel 1 (
    echo.
    echo ERROR: Android APK build failed.
    exit /b 1
)
copy /Y "UniTracks.Maui\bin\Release\%ANDROID_TFM%\publish\com.agredoapplication.unitracks-Signed.apk" ^
        "%DIST%\UniTracks-%VERSION%-android.apk"
echo  -^> %DIST%\UniTracks-%VERSION%-android.apk
echo.

REM ---------------------------------------------------------------------------
REM [2/3] Windows MSIX x64
REM   NOTE: -p:PlatformTarget=x64 als globaler CLI-Schalter, damit ALLE
REM         referenzierten Projekte fuer x64 kompiliert werden (sonst host-arch).
REM   NOTE: Klassenbibliotheken teilen einen generischen Windows-TFM-Outputpfad;
REM         vor jedem Publish loeschen, damit nicht die falsche Architektur
REM         wiederverwendet wird (dotnet clean kaskadiert nicht zu Referenzen).
REM ---------------------------------------------------------------------------
echo [2/3] Building Windows MSIX x64...
echo  [pre-clean] Removing Windows TFM class-library outputs...
for /d %%p in (UniTracks.*) do (
    if /I not "%%p"=="UniTracks.Maui" (
        if exist "%%p\bin\Release\%WINDOWS_TFM%" rd /s /q "%%p\bin\Release\%WINDOWS_TFM%"
        if exist "%%p\obj\Release\%WINDOWS_TFM%" rd /s /q "%%p\obj\Release\%WINDOWS_TFM%"
    )
)
dotnet publish %PROJECT% -f %WINDOWS_TFM% -p:PublishProfile=MSIX-win-x64 -p:PlatformTarget=x64
if errorlevel 1 (
    echo.
    echo ERROR: MSIX x64 build failed.
    exit /b 1
)
for /r "UniTracks.Maui\bin\x64\Release\%WINDOWS_TFM%\win-x64\AppPackages" %%f in (*.msix *.msixbundle) do (
    copy /Y "%%f" "%DIST%\UniTracks-%VERSION%-x64.msix"
    echo  -^> %DIST%\UniTracks-%VERSION%-x64.msix
)
echo.

REM ---------------------------------------------------------------------------
REM [3/3] Windows MSIX ARM64
REM   NOTE: gleiche pre-clean Begruendung wie bei x64 – sonst landen x64-DLLs
REM         im ARM64-Paket.
REM ---------------------------------------------------------------------------
echo [3/3] Building Windows MSIX ARM64...
echo  [pre-clean] Removing Windows TFM class-library outputs...
for /d %%p in (UniTracks.*) do (
    if /I not "%%p"=="UniTracks.Maui" (
        if exist "%%p\bin\Release\%WINDOWS_TFM%" rd /s /q "%%p\bin\Release\%WINDOWS_TFM%"
        if exist "%%p\obj\Release\%WINDOWS_TFM%" rd /s /q "%%p\obj\Release\%WINDOWS_TFM%"
    )
)
dotnet publish %PROJECT% -f %WINDOWS_TFM% -p:PublishProfile=MSIX-win-arm64 -p:PlatformTarget=arm64
if errorlevel 1 (
    echo.
    echo ERROR: MSIX ARM64 build failed.
    exit /b 1
)
for /r "UniTracks.Maui\bin\ARM64\Release\%WINDOWS_TFM%\win-arm64\AppPackages" %%f in (*.msix *.msixbundle) do (
    copy /Y "%%f" "%DIST%\UniTracks-%VERSION%-arm64.msix"
    echo  -^> %DIST%\UniTracks-%VERSION%-arm64.msix
)
echo.

REM ---------------------------------------------------------------------------
echo ========================================
echo  All artifacts built successfully
echo ========================================
echo.
echo Artifacts in %DIST%\:
dir /b %DIST%\UniTracks-%VERSION%-*
echo.
echo iOS IPA: kann nur auf einem Mac gebaut werden - siehe PACKAGING.md
echo.
endlocal
