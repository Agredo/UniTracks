namespace UniTracks.Games.BaseCamp;

/// <summary>Outcome of a camp module upgrade attempt against the camp engine.</summary>
public record UpgradeResult
{
    public bool Success { get; init; }

    /// <summary>User-facing failure reason ("" on success).</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    public string ModuleId { get; init; } = string.Empty;

    /// <summary>Level the module reaches when the upgrade succeeds.</summary>
    public int NewLevel { get; init; }

    /// <summary>Price actually paid (supplies + coins).</summary>
    public CampCost Cost { get; init; } = CampCost.Free;

    public static UpgradeResult Fail(string message) => new() { ErrorMessage = message };

    public static UpgradeResult Ok(string moduleId, int newLevel, CampCost cost) =>
        new() { Success = true, ModuleId = moduleId, NewLevel = newLevel, Cost = cost };
}
