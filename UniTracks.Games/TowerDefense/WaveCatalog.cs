namespace UniTracks.Games.TowerDefense;

/// <summary>
/// Procedural wave composition. Waves scale endlessly: later waves add tougher enemy
/// types and multiply hit points, every fifth wave sends a hornet as boss.
/// </summary>
public static class WaveCatalog
{
    /// <summary>Milliseconds between two enemy spawns within a wave.</summary>
    public const int SpawnIntervalMs = 800;

    /// <summary>Hit-point multiplier per wave (wave 1 = 100 %, growing by 18 % each wave).</summary>
    public static double HpMultiplier(int wave) => 1 + 0.18 * (wave - 1);

    /// <summary>
    /// The enemy types spawning in the given wave, in spawn order. Waves stay endless —
    /// composition and hit points keep scaling. Tougher types (gnats, wasps) arrive earlier
    /// so the free starter tower falls behind and players must unlock stronger towers.
    /// Harder maps add a few extra swarmers on top of the per-wave scaling.
    /// </summary>
    public static IReadOnlyList<EnemyDefinition> For(int wave, DefenseMap map)
    {
        var enemies = new List<EnemyDefinition>();

        int extraSwarm = Math.Max(0, map.Difficulty - 1);
        int mosquitoes = 3 + wave + extraSwarm;
        int gnats = wave >= 2 ? wave : 0;
        int wasps = wave >= 4 ? (wave - 2) : 0;

        for (int i = 0; i < mosquitoes; i++)
        {
            enemies.Add(EnemyCatalog.Find("mosquito")!);
        }

        for (int i = 0; i < gnats; i++)
        {
            enemies.Add(EnemyCatalog.Find("gnat")!);
        }

        for (int i = 0; i < wasps; i++)
        {
            enemies.Add(EnemyCatalog.Find("wasp")!);
        }

        if (wave % 5 == 0)
        {
            enemies.Add(EnemyCatalog.Find("hornet")!);
        }

        return enemies;
    }
}
