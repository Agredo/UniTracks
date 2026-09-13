import ActivityKit
import Foundation

// The C-callable surface the .NET app uses. Everything is `@_cdecl` with plain C types -
// `int32_t` instead of `Bool` - so there is no question about how .NET marshals the arguments.
//
// Nothing here throws to the caller: a Live Activity is a nice-to-have surface, and a failure must
// never disturb the recording. Failures are reported as 0.

/// Signature of the handler C# registers: it receives a `RunActivityCommand` raw value.
public typealias LACommandHandler = @convention(c) (Int32) -> Void

private var commandHandler: LACommandHandler?
private var isObservingCommands = false

// A button tap crosses a process boundary: the widget extension draws the card, but only the app
// knows the recording. A Darwin notification carries the tap over without any entitlement (an app
// group would need one) and arrives even when the system performs the intent inside the extension -
// which is what it does here, so the in-process call alone ends up in a process that has no handler.
//
// The names cannot be derived from `Bundle.main`: app and extension have different bundle ids.
private let commandNotificationPrefix = "com.agredoapplication.unitracks.liveactivity.command."
private let darwinCenter = CFNotificationCenterGetDarwinNotifyCenter()

private func notificationName(for command: RunActivityCommand) -> String {
    switch command {
    case .pause: return commandNotificationPrefix + "pause"
    case .resume: return commandNotificationPrefix + "resume"
    case .stop: return commandNotificationPrefix + "stop"
    }
}

// Guards against a tap being counted twice: the direct call and the notification both fire when the
// system happens to perform the intent inside the app itself, and they can overtake each other. A
// second identical command within this window is dropped - the app ignores duplicates by state anyway.
private let duplicateWindow: TimeInterval = 1

private var lastCommand: Int32?
private var lastCommandAt = Date.distantPast
private let deliveryLock = NSLock()

private func deliver(_ rawCommand: Int32) {
    guard let commandHandler else { return }

    deliveryLock.lock()
    defer { deliveryLock.unlock() }

    let now = Date()
    if lastCommand == rawCommand, now.timeIntervalSince(lastCommandAt) < duplicateWindow {
        logNative("Befehl \(rawCommand) verworfen (doppelt).")
        return
    }

    lastCommand = rawCommand
    lastCommandAt = now
    logNative("Befehl \(rawCommand) an die App uebergeben.")
    commandHandler(rawCommand)
}

// The command travels as the observer value instead of a payload - a Darwin notification carries
// none, and the value 1...3 keeps every allocated object out of the observer's lifetime.
private let commandReceived: CFNotificationCallback = { _, observer, _, _, _ in
    guard let observer else { return }

    let rawCommand = Int32(UInt(bitPattern: observer))
    logNative("Befehl \(rawCommand) aus einer Mitteilung empfangen.")
    deliver(rawCommand)
}

/// Registers the C# callback for Lock Screen button taps and starts listening for the taps that the
/// widget extension has to hand over (see `dispatchCommand`).
@_cdecl("LA_SetCommandHandler")
public func LA_SetCommandHandler(_ handler: LACommandHandler?) {
    commandHandler = handler
    logNative(handler == nil ? "Handler abgemeldet." : "Handler angemeldet.")

    guard handler != nil, !isObservingCommands else { return }
    isObservingCommands = true

    for command in [RunActivityCommand.pause, .resume, .stop] {
        CFNotificationCenterAddObserver(
            darwinCenter,
            UnsafeMutableRawPointer(bitPattern: UInt(command.rawValue)),
            commandReceived,
            notificationName(for: command) as CFString,
            nil,
            .deliverImmediately)
    }
}

/// Called from the card's buttons (see RunControlIntents.swift).
public func dispatchCommand(_ command: RunActivityCommand) {
    logNative("Knopf \(command.rawValue) gedrueckt.")

    // When the system performs the intent in the app itself, this is the whole way ...
    deliver(command.rawValue)

    // ... and this is how a tap made in the widget extension reaches the app.
    CFNotificationCenterPostNotification(
        darwinCenter,
        CFNotificationName(notificationName(for: command) as CFString),
        nil,
        nil,
        true)
}

/// Whether the user allows Live Activities for UniTracks (Settings > UniTracks > Live Activities) and
/// the OS is new enough. The app falls back to its notification surface when this is 0.
@_cdecl("LA_IsSupported")
public func LA_IsSupported() -> Int32 {
    guard #available(iOS 16.1, *) else { return 0 }
    return ActivityAuthorizationInfo().areActivitiesEnabled ? 1 : 0
}

/// Publishes the recording state: creates the card if none is showing, otherwise updates it.
@_cdecl("LA_Publish")
public func LA_Publish(_ startedAt: Double, _ recordedSeconds: Double, _ paused: Int32) -> Int32 {
    guard #available(iOS 16.1, *) else { return 0 }
    guard ActivityAuthorizationInfo().areActivitiesEnabled else {
        NSLog("UniTracks LiveActivity: activities are disabled in Settings")
        return 0
    }

    let state = RunActivityAttributes.ContentState(
        startedAt: startedAt,
        recordedSeconds: recordedSeconds,
        paused: paused != 0
    )

    if let running = currentActivity() {
        Task { await running.update(using: state) }
        return 1
    }

    do {
        _ = try Activity.request(
            attributes: RunActivityAttributes(),
            contentState: state,
            pushType: nil
        )
        return 1
    } catch {
        NSLog("UniTracks LiveActivity: start failed: \(error)")
        return 0
    }
}

/// Takes the card down again. The dismissal policy needs iOS 16.2, the version from which a card can
/// be removed immediately instead of lingering on the Lock Screen.
@_cdecl("LA_End")
public func LA_End() -> Int32 {
    guard #available(iOS 16.2, *) else { return 0 }
    guard let running = currentActivity() else { return 0 }

    Task { await running.end(nil, dismissalPolicy: .immediate) }
    return 1
}

@available(iOS 16.1, *)
private func currentActivity() -> Activity<RunActivityAttributes>? {
    Activity<RunActivityAttributes>.activities.first { $0.activityState == .active }
}

/// Appends one line to `<container>/Documents/UniTracks/liveactivity.log`.
///
/// A button tap is handled by one of two processes, and the extension has no console at all, so a tap
/// that goes nowhere would leave no trace. Writing into whichever container this process owns also
/// shows which of the two ran the tap. Never throws: diagnostics must not cost the tap.
private func logNative(_ message: String) {
    guard let documents = FileManager.default
        .urls(for: .documentDirectory, in: .userDomainMask).first else { return }

    let directory = documents.appendingPathComponent("UniTracks", isDirectory: true)
    let file = directory.appendingPathComponent("liveactivity.log")
    let line = "[\(Date())] \(message)\n"

    do {
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)

        let size = (try? FileManager.default.attributesOfItem(atPath: file.path))?[.size] as? Int ?? 0
        if size > 64 * 1024 {
            try FileManager.default.removeItem(at: file)
        }

        if let handle = try? FileHandle(forWritingTo: file) {
            defer { try? handle.close() }
            try handle.seekToEnd()
            try handle.write(contentsOf: Data(line.utf8))
        } else {
            try Data(line.utf8).write(to: file)
        }
    } catch {
        // Ignored on purpose (see above).
    }
}
