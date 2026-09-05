namespace UniTracks.Games.Shared.Economy;

/// <summary>
/// Per-trip-type distance factors for the coin economy. A road cyclist covers the same
/// kilometers with far less effort than a jogger — so coins per km scale by category.
/// The table lives only here (games layer, server-side): no client input can influence it.
/// </summary>
public static class TripTypeFactors
{
    /// <summary>Factor for running/walking categories — the reference effort.</summary>
    public const double Running = 1.0;

    /// <summary>Slow and exhausting — swimmers deserve a bonus.</summary>
    public const double WaterSports = 1.2;

    /// <summary>Physical, but lift-assisted / downhill-heavy.</summary>
    public const double WinterSports = 0.8;

    /// <summary>Wheels reduce rolling resistance.</summary>
    public const double Skating = 0.7;

    /// <summary>Mostly indoor activities where distance is secondary.</summary>
    public const double Indoor = 0.5;

    /// <summary>The bicycle's mechanical advantage.</summary>
    public const double Cycling = 0.3;

    /// <summary>Motor-assisted trip types (matched by identifier, not category).</summary>
    public const double MotorAssisted = 0.1;

    /// <summary>Identifiers of motor-assisted trip types inside the cycling category.</summary>
    private static readonly HashSet<string> MotorAssistedIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "ebikeride",
        "emountainbikeride",
    };

    /// <summary>
    /// Resolves the coin factor for a trip type. The category comes from the seed catalog
    /// (triptypes.json); motor-assisted identifiers override their category factor.
    /// Unknown categories fall back to <see cref="Running"/>.
    /// </summary>
    public static double For(string? category, string? identifier)
    {
        if (identifier is not null && MotorAssistedIdentifiers.Contains(identifier))
        {
            return MotorAssisted;
        }

        return category switch
        {
            "running" => Running,
            "water sports" => WaterSports,
            "winter sports" => WinterSports,
            "skating" => Skating,
            "cycling" => Cycling,
            "fitness" or "fighting sports" or "ball sports" or "miscellaneous" => Indoor,
            _ => Running,
        };
    }
}
