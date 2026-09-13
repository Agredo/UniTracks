import SwiftUI
import WidgetKit

// Entry point of the widget extension. It exists only for the Live Activity - UniTracks has no home
// screen widgets - so the bundle lists exactly one widget.
@main
struct UniTracksWidgetBundle: WidgetBundle {
    var body: some Widget {
        UniTracksRunWidget()
    }
}
