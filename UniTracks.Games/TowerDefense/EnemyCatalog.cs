namespace UniTracks.Games.TowerDefense;

/// <summary>Static catalog of all enemy types for the trail defense game.</summary>
public static class EnemyCatalog
{
    public static IReadOnlyList<EnemyDefinition> Enemies { get; } = new List<EnemyDefinition>
    {
        new() { Id = "mosquito", Name = "Mücke", Icon = "🦟", BaseHp = 10, SpeedTilesPerSecond = 1.2, EnergyReward = 0, ScoreReward = 5, LeakDamage = 2 },
        new() { Id = "gnat", Name = "Schnake", Icon = "🪰", BaseHp = 30, SpeedTilesPerSecond = 0.85, EnergyReward = 0, ScoreReward = 10, LeakDamage = 2 },
        new() { Id = "wasp", Name = "Wespe", Icon = "🐝", BaseHp = 55, SpeedTilesPerSecond = 1.6, EnergyReward = 0, ScoreReward = 20, LeakDamage = 2 },
        new() { Id = "hornet", Name = "Hornisse", Icon = "🪲", BaseHp = 220, SpeedTilesPerSecond = 0.65, EnergyReward = 0, ScoreReward = 60, LeakDamage = 6 },
    };

    public static EnemyDefinition? Find(string id) => Enemies.FirstOrDefault(e => e.Id == id);
}
