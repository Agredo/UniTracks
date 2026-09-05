namespace UniTracks.Games.TowerDefense;

/// <summary>
/// Procedural wave composition. Waves scale endlessly: later waves add tougher enemy
/// types and multiply hit points, every fifth wave sends a hornet as boss.
/// </summary>
public static class WaveCatalog
{
    /// <summary>Milliseconds between two enemy spawns within a wave.</summary>
    public const int SpawnIntervalMs = 800;

    /// <summary>Hit-point multiplier per wave (wave 1 = 100 %, growing by 15 % each wave).</summary>
    public static double HpMultiplier(int wave) => 1 + 0.15 * (wave - 1);

    /// <summary>Energy bonus awarded when a wave is fully cleared.</summary>
    public static int ClearBonus(int wave) => 20 + 5 * wave;

    /// <summary>
    /// The enemy types spawning in the given wave, in spawn order. Waves stay endless —
    /// composition and hit points simply keep scaling.
    /// </summary>
    public static IReadOnlyList<EnemyDefinition> For(int wave)
    {
        var enemies = new List<EnemyDefinition>();

        int mosquitoes = 4 + wave;
        int gnats = wave >= 3 ? (wave - 1) : 0;
        int wasps = wave >= 5 ? (wave - 3) : 0;

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
