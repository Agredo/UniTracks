namespace UniTracks.Games.TowerDefense;

/// <summary>Outcome of a permanent tower unlock purchase.</summary>
public record UnlockResult
{
    private UnlockResult(string? errorMessage)
    {
        ErrorMessage = errorMessage ?? string.Empty;
    }

    public bool Success => ErrorMessage.Length == 0;

    /// <summary>User-facing failure reason ("" on success).</summary>
    public string ErrorMessage { get; }

    public static UnlockResult Ok() => new((string?)null);

    public static UnlockResult Fail(string message) => new(message);
}
