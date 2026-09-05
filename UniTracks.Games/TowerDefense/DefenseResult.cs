namespace UniTracks.Games.TowerDefense;

/// <summary>Validation errors for in-run actions, mapped to user-facing messages.</summary>
public enum DefenseError
{
    UnknownTower,
    TowerLocked,
    OutOfBounds,
    NotBuildable,
    TileOccupied,
    NotEnoughEnergy,
    TileEmpty,
    WaveRunning,
}

/// <summary>
/// Outcome of a validated in-run action (placement, sell). Mirrors the city builder's
/// <c>PlaceResult</c> pattern: nothing is mutated on failure.
/// </summary>
public record DefenseResult
{
    private DefenseResult(DefenseError? error, int energyDelta)
    {
        Error = error;
        EnergyDelta = energyDelta;
    }

    public bool Success => Error is null;

    public DefenseError? Error { get; }

    /// <summary>Energy gained (sell refund) or spent (placement) by the action.</summary>
    public int EnergyDelta { get; }

    public string ErrorMessage => Error switch
    {
        DefenseError.UnknownTower => "Unbekannter Turm.",
        DefenseError.TowerLocked => "Dieser Turm ist noch nicht freigeschaltet.",
        DefenseError.OutOfBounds => "Außerhalb des Spielfelds.",
        DefenseError.NotBuildable => "Auf dem Trail kann nicht gebaut werden.",
        DefenseError.TileOccupied => "Dieses Feld ist bereits belegt.",
        DefenseError.NotEnoughEnergy => "Nicht genug Energie — besiege erst ein paar Mücken!",
        DefenseError.TileEmpty => "Hier steht kein Turm.",
        DefenseError.WaveRunning => "Warte, bis die Welle vorbei ist.",
        _ => string.Empty,
    };

    public static DefenseResult Ok(int energyDelta) => new(null, energyDelta);

    public static DefenseResult Fail(DefenseError error) => new(error, 0);
}
