// swift-tools-version: 5.10
import PackageDescription

// The bridge exists because .NET for iOS does not bind ActivityKit: C# can only reach the Live
// Activity API through plain C entry points, which this package exports.
//
// It is shared by both consumers:
//   * UniTracks.Maui links it as a static XCFramework (see UniTracks.Maui.csproj) and calls the LA_* functions.
//   * the widget extension builds it from this package, so the activity's attributes type is the very
//     same Swift type in both processes - which is what makes an update match the running activity.
//
// The library is static on purpose. A dynamic one would have to travel inside the app bundle and be
// found by both the app and the extension through @rpath, which the official sample only gets away
// with while running from Xcode; statically linking the same source into both binaries avoids the
// embedded-framework and signing steps entirely.
//
// The default target stays at iOS 16.1 (where ActivityKit appeared); the widget extension itself
// requires iOS 17 because only from there can a Live Activity carry tappable buttons.
let package = Package(
    name: "LiveActivityBridge",
    platforms: [.iOS(.v16)],
    products: [
        .library(name: "LiveActivityBridge", type: .static, targets: ["LiveActivityBridge"])
    ],
    targets: [
        .target(name: "LiveActivityBridge", path: "Sources")
    ]
)
