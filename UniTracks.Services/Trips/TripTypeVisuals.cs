using UniTracks.Models.Trip;

namespace UniTracks.Services.Trips;

/// <summary>
/// How a trip type looks on the trip card: icon and a muted accent pair (strong + soft background).
/// The accent comes from the category so the list stays calm; the icon can be specialised per
/// identifier (a paw for "Gassi gehen", a sailboat for sailing, ...).
/// </summary>
public record TripTypeVisual(string Icon, string Accent, string Soft);

public static class TripTypeVisuals
{
    private static readonly TripTypeVisual Default = new("run.png", "#A9B8AC", "#1F2A22");

    private static readonly Dictionary<string, TripTypeVisual> ByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["running"] = new("run.png", "#4DE790", "#1B3A29"),
        ["cycling"] = new("bike.png", "#4DC3FF", "#16303F"),
        ["winter sports"] = new("ski.png", "#B8E6FF", "#233642"),
        ["skating"] = new("skate.png", "#B79CFF", "#26203A"),
        ["water sports"] = new("swim.png", "#3ED6C5", "#14332E"),
        ["miscellaneous"] = new("mountain.png", "#D9C39D", "#33301F"),
        ["fitness"] = new("fitness.png", "#FFB04D", "#3A2E1B"),
        ["fighting sports"] = new("fight.png", "#FF4D5E", "#3A1B20"),
        ["ball sports"] = new("ball.png", "#C6E84D", "#2B3316"),
    };

    private static readonly Dictionary<string, string> IconByIdentifier = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trailrun"] = "mountain.png",
        ["walk"] = "walk.png",
        ["dogwalk"] = "dog.png",
        ["hiking"] = "hike.png",

        ["swimming"] = "swim.png",
        ["openwaterswimming"] = "swim.png",
        ["poolswimming"] = "swim.png",
        ["lapswimming"] = "swim.png",
        ["kanu"] = "kayak.png",
        ["kayak"] = "kayak.png",
        ["standuppaddling"] = "kayak.png",
        ["rowing"] = "kayak.png",
        ["dragonboat"] = "kayak.png",
        ["sailing"] = "sail.png",
        ["surfing"] = "surf.png",
        ["kitesurfing"] = "surf.png",
        ["windsurfing"] = "surf.png",
        ["wakeboarding"] = "surf.png",
        ["wakesurfing"] = "surf.png",
        ["waterskiing"] = "surf.png",
        ["diving"] = "dive.png",
        ["freediving"] = "dive.png",

        ["skiing"] = "ski.png",
        ["alpineskiing"] = "ski.png",
        ["backcountryskiing"] = "ski.png",
        ["telemarkskiing"] = "ski.png",
        ["crosscountryskiing"] = "ski.png",
        ["snowboarding"] = "snowboard.png",
        ["snowshoeing"] = "hike.png",
        ["snowshoehike"] = "hike.png",

        ["iceskating"] = "iceskate.png",

        ["yoga"] = "yoga.png",
        ["pilates"] = "yoga.png",
        ["barre"] = "yoga.png",

        ["climbing"] = "mountain.png",
        ["bouldering"] = "mountain.png",
        ["indoorclimbing"] = "mountain.png",
        ["outdoorclimbing"] = "mountain.png",
        ["iceclimbing"] = "mountain.png",
        ["mountaineering"] = "mountain.png",
        ["viaferrata"] = "mountain.png",
        ["canyoning"] = "mountain.png",
        ["golf"] = "golf.png",
        ["skateboarding"] = "skateboard.png",
        ["longboarding"] = "skateboard.png",
    };

    public static TripTypeVisual For(TripType? type)
    {
        if (type is null)
        {
            return Default;
        }

        var category = ByCategory.TryGetValue(type.Category ?? string.Empty, out var visual)
            ? visual
            : Default;

        return IconByIdentifier.TryGetValue(type.Identifier ?? string.Empty, out var icon)
            ? category with { Icon = icon }
            : category;
    }
}
