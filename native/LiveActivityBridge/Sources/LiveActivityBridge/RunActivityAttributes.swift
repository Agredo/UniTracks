import Foundation
import ActivityKit

/// The Live Activity's dynamic state. UniTracks records one trip at a time, so the activity needs no
/// identifying attributes - only the state below.
///
/// C# never declares this type itself (it passes the values to the bridge), but the bridge and the
/// widget extension must agree on it, because ActivityKit decides where an update belongs through the
/// activity's type. That is why the type lives in this package and the widget imports it instead of
/// declaring its own copy.
public struct RunActivityAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        /// Epoch seconds of the recording's start, counted as if it had never been paused. The widget
        /// draws a live clock from this value, so no update per second is needed - iOS would throttle
        /// them anyway.
        public var startedAt: Double

        /// Seconds already recorded. Shown while paused, where a running clock would be misleading.
        public var recordedSeconds: Double

        /// True while the recording is suspended; the card then offers "Fortsetzen" instead of "Pausieren".
        public var paused: Bool

        public init(startedAt: Double, recordedSeconds: Double, paused: Bool) {
            self.startedAt = startedAt
            self.recordedSeconds = recordedSeconds
            self.paused = paused
        }
    }

    public init() {}
}

/// The commands a card button can send. The raw values are part of the contract with C#
/// (see LiveActivityBridge.cs) and must not be renumbered.
public enum RunActivityCommand: Int32 {
    case pause = 1
    case resume = 2
    case stop = 3
}
