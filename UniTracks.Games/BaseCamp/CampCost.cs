namespace UniTracks.Games.BaseCamp;

/// <summary>
/// Price of one camp module upgrade. Supplies are the camp's own resource (produced by
/// time, but only as fast as the player's real activity allows); coins come from the
/// shared account every other game uses as well.
/// </summary>
public record CampCost
{
    /// <summary>Supplies paid from the harvested camp stock.</summary>
    public int Supplies { get; init; }

    /// <summary>Coins paid from the shared coin balance.</summary>
    public int Coins { get; init; }

    /// <summary>Nothing to pay — used for the free prestige module.</summary>
    public static CampCost Free => new();

    public bool IsFree => Supplies == 0 && Coins == 0;
}
