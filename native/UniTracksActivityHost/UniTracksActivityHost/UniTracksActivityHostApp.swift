import SwiftUI

// This "app" is scaffolding, not a product: iOS only allows a widget extension to be built and
// embedded as part of an application target, so Xcode needs one here. The app that ships is the MAUI
// app in UniTracks.Maui - it takes the built .appex from this target's PlugIns folder (see
// scripts/build-liveactivity.sh). Nothing in this file is ever launched.
@main
struct UniTracksActivityHostApp: App {
    var body: some Scene {
        WindowGroup {
            Text("UniTracks activity host")
        }
    }
}
