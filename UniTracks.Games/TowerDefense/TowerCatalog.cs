namespace UniTracks.Games.TowerDefense;

/// <summary>Static shop catalog of all unlockable towers for the trail defense game.</summary>
public static class TowerCatalog
{
    public static IReadOnlyList<TowerDefinition> Towers { get; } = new List<TowerDefinition>
    {
        new() { Id = "spray", Name = "Mückenspray", Description = "Schnelle Sprühstöße auf kurze Distanz.", Icon = "🧴", UnlockCost = 0, EnergyCost = 30, RangeTiles = 2.0, Damage = 4, FireIntervalMs = 550, ColorHex = "#4FC3F7", AttackStyle = AttackStyle.Spray },
        new() { Id = "candle", Name = "Duftkerze", Description = "Räuchert gleichmäßig alles in der Nähe aus.", Icon = "🕯️", UnlockCost = 150, RequiredLevel = 2, EnergyCost = 60, RangeTiles = 2.5, Damage = 8, FireIntervalMs = 900, ColorHex = "#FFB74D", AttackStyle = AttackStyle.Cloud },
        new() { Id = "zapper", Name = "Elektro-Falle", Description = "Langsam, aber vernichtend.", Icon = "⚡", UnlockCost = 400, RequiredLevel = 3, EnergyCost = 110, RangeTiles = 2.0, Damage = 22, FireIntervalMs = 1600, ColorHex = "#FFF176", AttackStyle = AttackStyle.Zap },
        new() { Id = "frog", Name = "Frosch", Description = "Schnappt weit entfernte Mücken aus der Luft.", Icon = "🐸", UnlockCost = 650, RequiredLevel = 4, EnergyCost = 160, RangeTiles = 3.5, Damage = 14, FireIntervalMs = 1100, ColorHex = "#81C784", AttackStyle = AttackStyle.Tongue },
        new() { Id = "gecko", Name = "Gecko", Description = "Der Endgegner für jeden Mückenschwarm.", Icon = "🦎", UnlockCost = 1000, RequiredLevel = 5, RequiredAchievementId = "hundred-km", EnergyCost = 260, RangeTiles = 3.0, Damage = 38, FireIntervalMs = 1500, ColorHex = "#BA68C8", AttackStyle = AttackStyle.Tongue },
    };

    public static TowerDefinition? Find(string id) => Towers.FirstOrDefault(t => t.Id == id);
}
