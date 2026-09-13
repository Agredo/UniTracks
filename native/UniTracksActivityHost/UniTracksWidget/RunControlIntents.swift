import AppIntents
import LiveActivityBridge

// Each button of the card is a `LiveActivityIntent`, so the tap does not launch the app. It only
// hands the command to `dispatchCommand`, which passes it to the app process; the app decides what
// happens and pushes the new state back into the card.

struct PauseRunIntent: LiveActivityIntent {
    static var title: LocalizedStringResource = "Pausieren"
    static var isDiscoverable: Bool = false

    func perform() async throws -> some IntentResult {
        dispatchCommand(.pause)
        return .result()
    }
}

struct ResumeRunIntent: LiveActivityIntent {
    static var title: LocalizedStringResource = "Fortsetzen"
    static var isDiscoverable: Bool = false

    func perform() async throws -> some IntentResult {
        dispatchCommand(.resume)
        return .result()
    }
}

struct StopRunIntent: LiveActivityIntent {
    static var title: LocalizedStringResource = "Stoppen"
    static var isDiscoverable: Bool = false

    func perform() async throws -> some IntentResult {
        dispatchCommand(.stop)
        return .result()
    }
}
