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
        new() { Id = "playground", Name = "Spielplatz", Description = "Schaukeln, rutschen, toben.", Icon = "🛝", Cost = 90, Theme = BuildingTheme.Leisure },
        new() { Id = "cafe", Name = "Café", Description = "Der beste Kaffee der Stadt.", Icon = "☕", Cost = 100, Theme = BuildingTheme.Commercial },
        new() { Id = "shop", Name = "Laden", Description = "Alles, was das Herz begehrt.", Icon = "🏪", Cost = 120, Theme = BuildingTheme.Commercial },
        new() { Id = "villa", Name = "Villa", Description = "Für die feinen Leute.", Icon = "🏡", Cost = 150, Theme = BuildingTheme.Residential },
        new() { Id = "school", Name = "Schule", Description = "Hier wird gelernt und gelacht.", Icon = "🏫", Cost = 200, Theme = BuildingTheme.Civic },
        new() { Id = "hospital", Name = "Krankenhaus", Description = "Immer für dich da.", Icon = "🏥", Cost = 300, Theme = BuildingTheme.Civic },
    };

    public static BuildingDefinition? Find(string id) => Buildings.FirstOrDefault(b => b.Id == id);
}
