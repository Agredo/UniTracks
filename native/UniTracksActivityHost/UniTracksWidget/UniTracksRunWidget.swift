import ActivityKit
import LiveActivityBridge
import SwiftUI
import WidgetKit

// The card the athlete sees on the Lock Screen (and in the Dynamic Island) while a recording runs.
// It is deliberately read-only apart from the three buttons: the elapsed time is derived from the
// start timestamp, so the clock keeps ticking without the app pushing an update every second.

struct UniTracksRunWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: RunActivityAttributes.self) { context in
            LockScreenRecordingView(state: context.state)
                .activityBackgroundTint(Color.black.opacity(0.8))
                .activitySystemActionForegroundColor(.white)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    Image(systemName: "figure.run")
                        .font(.title2)
                        .foregroundStyle(.green)
                        .padding(.leading, 4)
                }

                DynamicIslandExpandedRegion(.trailing) {
                    ElapsedView(state: context.state)
                        .font(.title3.monospacedDigit())
                        .padding(.trailing, 4)
                }

                DynamicIslandExpandedRegion(.bottom) {
                    RecordingButtons(state: context.state)
                }
            } compactLeading: {
                Image(systemName: "figure.run")
                    .foregroundStyle(.green)
            } compactTrailing: {
                ElapsedView(state: context.state)
                    .monospacedDigit()
                    .frame(maxWidth: 60)
            } minimal: {
                Image(systemName: "figure.run")
                    .foregroundStyle(.green)
            }
        }
    }
}

private struct LockScreenRecordingView: View {
    let state: RunActivityAttributes.ContentState

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack(alignment: .center, spacing: 12) {
                Image(systemName: state.paused ? "pause.circle.fill" : "figure.run.circle.fill")
                    .font(.title)
                    .foregroundStyle(state.paused ? Color.orange : Color.green)

                VStack(alignment: .leading, spacing: 2) {
                    Text("UniTracks")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(state.paused ? "Aufnahme pausiert" : "Aufnahme läuft")
                        .font(.headline)
                        .foregroundStyle(.white)
                }

                Spacer()

                ElapsedView(state: state)
                    .font(.title2.monospacedDigit())
                    .foregroundStyle(.white)
            }

            RecordingButtons(state: state)
        }
        .padding(16)
    }
}

/// Shows a live clock while recording and the frozen time while paused.
private struct ElapsedView: View {
    let state: RunActivityAttributes.ContentState

    var body: some View {
        if state.paused {
            Text(Self.formatted(state.recordedSeconds))
        } else {
            Text(Date(timeIntervalSince1970: state.startedAt), style: .timer)
        }
    }

    static func formatted(_ seconds: Double) -> String {
        let total = Int(seconds.rounded())
        return String(format: "%02d:%02d:%02d", total / 3600, (total % 3600) / 60, total % 60)
    }
}

/// Pause/resume and stop. The buttons hand their command to the running app (see RunControlIntents),
/// which then pushes the new state back, so the card always shows what the recording actually does.
private struct RecordingButtons: View {
    let state: RunActivityAttributes.ContentState

    var body: some View {
        HStack(spacing: 10) {
            if state.paused {
                Button(intent: ResumeRunIntent()) {
                    Label("Fortsetzen", systemImage: "play.fill")
                        .frame(maxWidth: .infinity)
                }
                .tint(.green)
            } else {
                Button(intent: PauseRunIntent()) {
                    Label("Pausieren", systemImage: "pause.fill")
                        .frame(maxWidth: .infinity)
                }
                .tint(.orange)
            }

            Button(intent: StopRunIntent()) {
                Label("Stoppen", systemImage: "stop.fill")
                    .frame(maxWidth: .infinity)
            }
            .tint(.red)
        }
        .font(.subheadline.weight(.semibold))
    }
}
