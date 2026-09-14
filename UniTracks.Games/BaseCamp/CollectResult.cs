namespace UniTracks.Games.BaseCamp;

/// <summary>
/// Outcome of a harvest: how much was banked and the camp state that resulted from it.
/// Mirrors <c>PlaceResult</c> of the city builder (success flag, message, payload).
/// </summary>
public record CollectResult
{
    public bool Success { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Supplies banked by this harvest.</summary>
    public int Collected { get; init; }

    /// <summary>Camp state after the harvest (null when the harvest failed).</summary>
    public CampState? Camp { get; init; }

    public static CollectResult Ok(CampState camp, int collected) =>
        new() { Success = true, Camp = camp, Collected = collected };

    public static CollectResult Fail(string message) =>
        new() { ErrorMessage = message };
}
