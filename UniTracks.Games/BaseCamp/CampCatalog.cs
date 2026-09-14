namespace UniTracks.Games.BaseCamp;

/// <summary>
/// Static catalog of all camp modules. Levels are the only persisted module state,
/// so prices live here (games layer, server-side) and cannot be influenced by the client.
/// </summary>
public static class CampCatalog
{
    public const string TentId = "tent";
    public const string KitchenId = "kitchen";
    public const string CellarId = "cellar";
    public const string MapId = "map";
    public const string RadioId = "radio";
    public const string HutId = "hut";
    public const string FlagId = "flag";

    public static IReadOnlyList<CampModuleDefinition> Modules { get; } = new List<CampModuleDefinition>
    {
        new()
        {
            Id = TentId,
            Name = "Zeltplatz",
            Description = "Mehr Stellfläche für Vorräte — das Lager läuft später über.",
            Icon = "⛺",
            MaxLevel = 4,
            LevelCosts = new List<CampCost>
            {
                new() { Supplies = 150 },
                new() { Supplies = 400 },
                new() { Supplies = 900 },
                new() { Supplies = 1800 },
            },
        },
        new()
        {
            Id = KitchenId,
            Name = "Kochstelle",
            Description = "Warme Mahlzeiten — das Lager füllt sich auch ohne Bewegung schneller.",
            Icon = "🔥",
            MaxLevel = 4,
            LevelCosts = new List<CampCost>
            {
                new() { Supplies = 120 },
                new() { Supplies = 320 },
                new() { Supplies = 800 },
                new() { Supplies = 1900 },
            },
        },
        new()
        {
            Id = CellarId,
            Name = "Vorratskeller",
            Description = "Kühle Lagerung — nach Pausen bleibt mehr Leistung erhalten.",
            Icon = "🥫",
            MaxLevel = 3,
            RequiredLevel = 2,
            LevelCosts = new List<CampCost>
            {
                new() { Supplies = 600, Coins = 300 },
                new() { Supplies = 1400, Coins = 700 },
                new() { Supplies = 3000, Coins = 1500 },
            },
        },
        new()
        {
            Id = MapId,
            Name = "Karte",
            Description = "Bessere Routen — jeder Kilometer der Woche zählt mehr.",
            Icon = "🗺️",
            MaxLevel = 3,
            RequiredLevel = 2,
            LevelCosts = new List<CampCost>
            {
                new() { Coins = 300 },
                new() { Coins = 700 },
                new() { Coins = 1500 },
            },
        },
        new()
        {
            Id = RadioId,
            Name = "Funkgerät",
            Description = "Kontakt zur Basis — deine Streak zählt stärker.",
            Icon = "📻",
            MaxLevel = 2,
            RequiredLevel = 3,
            LevelCosts = new List<CampCost>
            {
                new() { Coins = 400 },
                new() { Coins = 900 },
            },
        },
        new()
        {
            Id = HutId,
            Name = "Basislager-Hütte",
            Description = "Ein festes Dach für die Basis — Vorräte fließen reichlicher.",
            Icon = "🏔️",
            MaxLevel = 1,
            RequiredLevel = 7,
            RequiredAchievementId = "streak-7",
            LevelCosts = new List<CampCost>
            {
                new() { Coins = 2000 },
            },
        },

        // Prestige module — unlocked by an achievement, purely decorative.
        new()
        {
            Id = FlagId,
            Name = "Gipfelfahne",
            Description = "Ein Zeichen für alle, die über 1000 m gestiegen sind.",
            Icon = "🏳️",
            MaxLevel = 1,
            RequiredAchievementId = "summit",
            LevelCosts = new List<CampCost>
            {
                CampCost.Free,
            },
        },
    };

    public static CampModuleDefinition? Find(string id) => Modules.FirstOrDefault(m => m.Id == id);
}
