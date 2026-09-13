#!/usr/bin/env bash
#
# Baut die nativen Teile der iOS-Live-Activity (Karte auf dem Sperrbildschirm waehrend einer
# Aufnahme) und legt sie dort ab, wo das MAUI-Projekt sie erwartet:
#
#   UniTracks.Maui/Platforms/iOS/LiveActivityBridge.xcframework
#       statische Swift-Bruecke (LA_*). Sie wird in den App-Prozess gelinkt und stellt dort
#       ActivityKit bereit, das .NET fuer iOS nicht bindet.
#
#   UniTracks.Maui/Platforms/iOS/PlugIns/UniTracksWidget.appex
#   UniTracks.Maui/Platforms/iOS/PlugIns/iphonesimulator/UniTracksWidget.appex
#       die Widget-Extension, die die Karte zeichnet und die Knoepfe bereitstellt.
#
# Aufruf:  scripts/build-liveactivity.sh [device|simulator|all]   (Standard: all)
#
# Muss vor einem iOS-Build gelaufen sein. Beide Ausgabepfade stehen in der .gitignore und werden
# bei jedem Lauf neu erzeugt.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BRIDGE_PKG="$ROOT/native/LiveActivityBridge"
HOST_DIR="$ROOT/native/UniTracksActivityHost"
HOST_PROJECT="$HOST_DIR/UniTracksActivityHost.xcodeproj"
BUILD_DIR="$ROOT/native/build"
DEST_DIR="$ROOT/UniTracks.Maui/Platforms/iOS"
TARGET="${1:-all}"

if [ "$TARGET" != "device" ] && [ "$TARGET" != "simulator" ] && [ "$TARGET" != "all" ]; then
    echo "Unbekanntes Ziel '$TARGET' (erlaubt: device, simulator, all)" >&2
    exit 1
fi

mkdir -p "$BUILD_DIR"

# Baut die Bruecke fuer ein Triple und gibt den Pfad der statischen Bibliothek aus.
build_bridge_library() {
    local triple="$1" sdk="$2" sdk_path
    sdk_path="$(xcrun --sdk "$sdk" --show-sdk-path)"

    (
        cd "$BRIDGE_PKG"
        swift build -c release --triple "$triple" --sdk "$sdk_path" >/dev/null
        echo "$(swift build -c release --triple "$triple" --sdk "$sdk_path" --show-bin-path)/libLiveActivityBridge.a"
    )
}

# Baut Host-App und Widget und kopiert das fertige .appex in den Zielordner.
# $1 Konfiguration, $2 SDK, $3 xcodebuild-Ziel, $4 Zielordner
build_widget() {
    local configuration="$1" sdk="$2" destination="$3" out_dir="$4"
    local derived="$BUILD_DIR/$configuration-$sdk"
    local appex="$derived/Build/Products/$configuration-$sdk/UniTracksActivityHost.app/PlugIns/UniTracksWidget.appex"

    xcodebuild \
        -project "$HOST_PROJECT" \
        -scheme UniTracksActivityHost \
        -configuration "$configuration" \
        -sdk "$sdk" \
        -destination "$destination" \
        -derivedDataPath "$derived" \
        CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO \
        build >/dev/null

    if [ ! -d "$appex" ]; then
        echo "Widget-Extension wurde nicht gebaut: $appex" >&2
        exit 1
    fi

    # Ohne diese Metadaten findet das System die App Intents hinter den Knoepfen nicht.
    if [ ! -d "$appex/Metadata.appintents" ]; then
        echo "Warnung: $appex enthaelt keine App-Intents-Metadaten." >&2
    fi

    mkdir -p "$out_dir"
    rm -rf "$out_dir/UniTracksWidget.appex"
    cp -R "$appex" "$out_dir/UniTracksWidget.appex"
    echo "  $out_dir/UniTracksWidget.appex"
}

echo "==> Statische Bruecke bauen"
DEVICE_LIB="$(build_bridge_library arm64-apple-ios16.0 iphoneos)"
SIM_ARM_LIB="$(build_bridge_library arm64-apple-ios16.0-simulator iphonesimulator)"
SIM_X64_LIB="$(build_bridge_library x86_64-apple-ios16.0-simulator iphonesimulator)"

# Der Simulator-Slice als Fat-Binary, damit der Simulator auf Intel und Apple Silicon laeuft.
rm -rf "$BUILD_DIR/libraries"
mkdir -p "$BUILD_DIR/libraries/device" "$BUILD_DIR/libraries/simulator"
cp "$DEVICE_LIB" "$BUILD_DIR/libraries/device/libLiveActivityBridge.a"
lipo -create "$SIM_ARM_LIB" "$SIM_X64_LIB" -output "$BUILD_DIR/libraries/simulator/libLiveActivityBridge.a"

rm -rf "$BUILD_DIR/LiveActivityBridge.xcframework"
xcodebuild -create-xcframework \
    -library "$BUILD_DIR/libraries/device/libLiveActivityBridge.a" \
    -library "$BUILD_DIR/libraries/simulator/libLiveActivityBridge.a" \
    -output "$BUILD_DIR/LiveActivityBridge.xcframework" >/dev/null

rm -rf "$DEST_DIR/LiveActivityBridge.xcframework"
cp -R "$BUILD_DIR/LiveActivityBridge.xcframework" "$DEST_DIR/LiveActivityBridge.xcframework"
echo "  $DEST_DIR/LiveActivityBridge.xcframework"

if [ "$TARGET" = "device" ] || [ "$TARGET" = "all" ]; then
    echo "==> Widget-Extension fuer das Geraet bauen (Release, iphoneos)"
    build_widget Release iphoneos "generic/platform=iOS" "$DEST_DIR/PlugIns"
fi

if [ "$TARGET" = "simulator" ] || [ "$TARGET" = "all" ]; then
    echo "==> Widget-Extension fuer den Simulator bauen (Debug, iphonesimulator)"
    build_widget Debug iphonesimulator "generic/platform=iOS Simulator" "$DEST_DIR/PlugIns/iphonesimulator"
fi

echo "==> Fertig"
