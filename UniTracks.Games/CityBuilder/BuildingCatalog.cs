namespace UniTracks.Games.CityBuilder;

/// <summary>Static shop catalog of all purchasable buildings for the cozy city builder.</summary>
public static class BuildingCatalog
{
    public static IReadOnlyList<BuildingDefinition> Buildings { get; } = new List<BuildingDefinition>
    {
        new() { Id = "flowerbed", Name = "Blumenbeet", Description = "Ein farbenfrohes Beet für die Nachbarn.", Icon = "🌷", Cost = 15, Theme = BuildingTheme.Nature },
        new() { Id = "tree", Name = "Baum", Description = "Spendet Schatten und gute Laune.", Icon = "🌳", Cost = 20, Theme = BuildingTheme.Nature },
        new() { Id = "pine", Name = "Nadelbaum", Description = "Der Klassiker aus dem Schwarzwald.", Icon = "🌲", Cost = 20, Theme = BuildingTheme.Nature },
        new() { Id = "fountain", Name = "Brunnen", Description = "Plätschert entspannt vor sich hin.", Icon = "⛲", Cost = 60, Theme = BuildingTheme.Water },
        new() { Id = "house", Name = "Haus", Description = "Ein gemütliches Zuhause.", Icon = "🏠", Cost = 80, Theme = BuildingTheme.Residential },
        new() { Id = "playground", Name = "Spielplatz", Description = "Schaukeln, rutschen, toben.", Icon = "🛝", Cost = 90, Theme = BuildingTheme.Leisure, RequiredLevel = 2 },
        new() { Id = "cafe", Name = "Café", Description = "Der beste Kaffee der Stadt.", Icon = "☕", Cost = 100, Theme = BuildingTheme.Commercial, RequiredLevel = 2 },
        new() { Id = "shop", Name = "Laden", Description = "Alles, was das Herz begehrt.", Icon = "🏪", Cost = 120, Theme = BuildingTheme.Commercial, RequiredLevel = 3 },
        new() { Id = "villa", Name = "Villa", Description = "Für die feinen Leute.", Icon = "🏡", Cost = 150, Theme = BuildingTheme.Residential, RequiredLevel = 3 },
        new() { Id = "school", Name = "Schule", Description = "Hier wird gelernt und gelacht.", Icon = "🏫", Cost = 200, Theme = BuildingTheme.Civic, RequiredLevel = 4 },
        new() { Id = "hospital", Name = "Krankenhaus", Description = "Immer für dich da.", Icon = "🏥", Cost = 300, Theme = BuildingTheme.Civic, RequiredLevel = 5 },

        // Prestige buildings — unlocked by achievements, not levels.
        new() { Id = "goldenstatue", Name = "Goldene Statue", Description = "Für echte Kilometerkönige.", Icon = "🏆", Cost = 500, Theme = BuildingTheme.Leisure, RequiredAchievementId = "hundred-km" },
        new() { Id = "summitcross", Name = "Gipfel-Kreuz", Description = "Ein Stück Berggipfel für deine Stadt.", Icon = "⛰️", Cost = 400, Theme = BuildingTheme.Nature, RequiredAchievementId = "summit" },
        new() { Id = "marathonarch", Name = "Marathon-Torbogen", Description = "Das Zielband für Marathon-Helden.", Icon = "🎽", Cost = 400, Theme = BuildingTheme.Leisure, RequiredAchievementId = "marathon" },
    };

    public static BuildingDefinition? Find(string id) => Buildings.FirstOrDefault(b => b.Id == id);
}
