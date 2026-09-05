namespace UniTracks.Games.TowerDefense;

/// <summary>Static shop catalog of all unlockable towers for the trail defense game.</summary>
public static class TowerCatalog
{
    public static IReadOnlyList<TowerDefinition> Towers { get; } = new List<TowerDefinition>
    {
        new() { Id = "spray", Name = "Mückenspray", Description = "Schnelle Sprühstöße auf kurze Distanz.", Icon = "🧴", UnlockCost = 0, EnergyCost = 25, RangeTiles = 2.0, Damage = 4, FireIntervalMs = 550, ColorHex = "#4FC3F7", AttackStyle = AttackStyle.Spray },
        new() { Id = "candle", Name = "Duftkerze", Description = "Räuchert gleichmäßig alles in der Nähe aus.", Icon = "🕯️", UnlockCost = 100, EnergyCost = 45, RangeTiles = 2.5, Damage = 8, FireIntervalMs = 900, ColorHex = "#FFB74D", AttackStyle = AttackStyle.Cloud },
        new() { Id = "zapper", Name = "Elektro-Falle", Description = "Langsam, aber vernichtend.", Icon = "⚡", UnlockCost = 250, EnergyCost = 80, RangeTiles = 2.0, Damage = 22, FireIntervalMs = 1600, ColorHex = "#FFF176", AttackStyle = AttackStyle.Zap },
        new() { Id = "frog", Name = "Frosch", Description = "Schnappt weit entfernte Mücken aus der Luft.", Icon = "🐸", UnlockCost = 400, EnergyCost = 120, RangeTiles = 3.5, Damage = 14, FireIntervalMs = 1100, ColorHex = "#81C784", AttackStyle = AttackStyle.Tongue },
        new() { Id = "gecko", Name = "Gecko", Description = "Der Endgegner für jeden Mückenschwarm.", Icon = "🦎", UnlockCost = 700, EnergyCost = 200, RangeTiles = 3.0, Damage = 38, FireIntervalMs = 1500, ColorHex = "#BA68C8", AttackStyle = AttackStyle.Tongue },
    };

    public static TowerDefinition? Find(string id) => Towers.FirstOrDefault(t => t.Id == id);
}
