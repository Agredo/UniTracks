using UniTracks.Models.Trip;

namespace UniTracks.Services.Trips;

/// <summary>
/// Additional search terms for the trip type picker. The catalogue names are German, but the word a
/// user types is often a different one ("Fahrrad" for "Radfahren", "Joggen" for "Laufen"), so every
/// identifier can carry synonyms that the search matches in addition to name and identifier.
/// Identifiers stay stable, so this table never affects trip matching or the game economy.
/// </summary>
public static class TripTypeSearchTerms
{
    private static readonly Dictionary<string, string[]> AliasesByIdentifier = new(StringComparer.OrdinalIgnoreCase)
    {
        // running
        ["run"] = new[] { "joggen", "jogging" },
        ["trailrun"] = new[] { "trail", "waldlauf", "berglauf" },
        ["walk"] = new[] { "spazieren", "spaziergang", "walking" },
        ["dogwalk"] = new[] { "hund", "gassi" },
        ["hiking"] = new[] { "bergwandern", "trekking", "wander" },

        // cycling
        ["cycling"] = new[] { "fahrrad", "radeln", "rennrad", "bike" },
        ["mountainbiking"] = new[] { "mtb", "mountainbike", "fahrrad", "berg" },
        ["gravelride"] = new[] { "gravel", "schotter", "fahrrad" },
        ["ebikeride"] = new[] { "pedelec", "fahrrad" },
        ["emountainbikeride"] = new[] { "mtb", "pedelec", "fahrrad" },
        ["velobikeride"] = new[] { "velo", "fahrrad" },

        // winter sports
        ["skiing"] = new[] { "piste", "abfahrt", "ski" },
        ["snowboarding"] = new[] { "snowboard", "board" },
        ["crosscountryskiing"] = new[] { "loipe", "langlaufen" },
        ["backcountryskiing"] = new[] { "skitour", "tourengehen" },
        ["telemarkskiing"] = new[] { "ski" },
        ["snowshoeing"] = new[] { "schneeschuh", "winterwandern" },
        ["alpineskiing"] = new[] { "alpin", "piste", "abfahrt", "ski" },
        ["snowshoehike"] = new[] { "schneeschuh", "winterwandern" },

        // skating
        ["skating"] = new[] { "rollen", "rollern" },
        ["inlineskating"] = new[] { "inline", "rollen" },
        ["rollerskating"] = new[] { "rollschuh", "rollen" },
        ["iceskating"] = new[] { "eislaufen", "eislauf", "eis" },

        // water sports
        ["swimming"] = new[] { "baden", "schwimm" },
        ["openwaterswimming"] = new[] { "freiwasser", "see", "badesee" },
        ["poolswimming"] = new[] { "schwimmbad", "hallenbad", "pool" },
        ["lapswimming"] = new[] { "bahnen", "becken" },
        ["kanu"] = new[] { "paddeln", "kanadier", "boot" },
        ["kayak"] = new[] { "paddeln", "boot" },
        ["standuppaddling"] = new[] { "sup", "stehpaddeln", "paddeln" },
        ["rowing"] = new[] { "ruder", "boot" },
        ["dragonboat"] = new[] { "paddeln", "boot" },
        ["sailing"] = new[] { "segel", "boot" },
        ["surfing"] = new[] { "welle", "surf" },
        ["kitesurfing"] = new[] { "kite", "surf" },
        ["windsurfing"] = new[] { "windsurf", "surf" },
        ["wakeboarding"] = new[] { "wake", "board" },
        ["wakesurfing"] = new[] { "wake", "surf" },
        ["waterskiing"] = new[] { "wasserski", "boot" },
        ["jetskiing"] = new[] { "jetski", "motor" },
        ["diving"] = new[] { "tauchen", "gerätetauchen" },
        ["freediving"] = new[] { "apnoe", "tauchen" },

        // miscellaneous
        ["horsebackriding"] = new[] { "pferd", "ausritt", "reiten" },
        ["climbing"] = new[] { "klettern", "kletter" },
        ["bouldering"] = new[] { "boulder", "klettern" },
        ["indoorclimbing"] = new[] { "kletterhalle", "halle", "klettern" },
        ["outdoorclimbing"] = new[] { "fels", "klettern" },
        ["iceclimbing"] = new[] { "eisklettern", "klettern" },
        ["mountaineering"] = new[] { "hochtour", "berg", "bergsteigen" },
        ["viaferrata"] = new[] { "klettersteig", "ferrata" },
        ["canyoning"] = new[] { "schlucht", "canyon" },
        ["skateboarding"] = new[] { "skate", "board" },
        ["longboarding"] = new[] { "longboard", "skate" },

        // fitness
        ["fitness"] = new[] { "gym", "krafttraining", "studio", "kraft" },
        ["crossfit"] = new[] { "kraft" },
        ["barre"] = new[] { "ballett" },
        ["zumba"] = new[] { "tanz" },
        ["dance"] = new[] { "tanz" },
        ["aerobics"] = new[] { "aerobic" },
        ["stepaerobics"] = new[] { "step", "aerobic" },
        ["spinning"] = new[] { "spinning", "indoor", "fahrrad" },
        ["indoorcycling"] = new[] { "indoor", "fahrrad", "spinning", "hometrainer" },

        // fighting sports
        ["boxing"] = new[] { "boxen", "faustkampf" },
        ["kickboxing"] = new[] { "kickboxen", "boxen" },
        ["martialarts"] = new[] { "kampfsport", "kampfkunst", "selbstverteidigung" },
        ["taekwondo"] = new[] { "kampfsport" },
        ["karate"] = new[] { "kampfsport" },
        ["judo"] = new[] { "kampfsport" },
        ["jiujitsu"] = new[] { "jiu", "jitsu", "kampfsport" },
        ["wrestling"] = new[] { "ringen", "kampfsport" },

        // ball sports
        ["football"] = new[] { "amerikanisch", "gridiron" },
        ["soccer"] = new[] { "fussball" },
        ["volleyball"] = new[] { "volley" },
        ["beachvolleyball"] = new[] { "beach", "strand", "volleyball" },
        ["tennis"] = new[] { "schläger" },
        ["tabletennis"] = new[] { "tischtennis", "ping", "platte" },
        ["badminton"] = new[] { "federball" },
        ["squash"] = new[] { "schläger" },
        ["racquetball"] = new[] { "schläger" },
        ["basketball"] = new[] { "korb" },
        ["americanfootball"] = new[] { "amerikanisch", "football", "gridiron" },
    };

    /// <summary>
    /// True when the type matches the search term by name, identifier or one of its German synonyms.
    /// A blank term matches everything.
    /// </summary>
    public static bool Matches(TripType type, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return true;
        }

        var value = term.Trim();

        if (type.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
            type.Identifier.Contains(value, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return AliasesByIdentifier.TryGetValue(type.Identifier, out var aliases) &&
               aliases.Any(alias => alias.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
