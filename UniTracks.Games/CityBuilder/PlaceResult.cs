namespace UniTracks.Games.CityBuilder;

/// <summary>Why a build/demolish attempt failed.</summary>
public enum PlaceError
{
    None,
    UnknownBuilding,
    OutOfBounds,
    TileOccupied,
    TileEmpty,
    NotEnoughCoins,
    LevelTooLow,
    AchievementLocked,
    MaxSizeReached,
}

/// <summary>Outcome of a placement or demolition attempt against the city engine.</summary>
public record PlaceResult
{
    public bool Success { get; init; }

    public PlaceError Error { get; init; } = PlaceError.None;

    public CityState? City { get; init; }

    /// <summary>Coins actually paid (placement) or refunded (demolition).</summary>
    public int CoinsDelta { get; init; }

    public static PlaceResult Fail(PlaceError error) => new() { Error = error };

    public static PlaceResult Ok(CityState city, int coinsDelta) =>
        new() { Success = true, City = city, CoinsDelta = coinsDelta };

    /// <summary>User-facing message for failed attempts.</summary>
    public string ErrorMessage => Error switch
    {
        PlaceError.UnknownBuilding => "Unbekanntes Gebäude.",
        PlaceError.OutOfBounds => "Außerhalb der Stadt.",
        PlaceError.TileOccupied => "Dieses Feld ist schon bebaut.",
        PlaceError.TileEmpty => "Hier steht nichts zum Abriss.",
        PlaceError.NotEnoughCoins => "Nicht genug Coins — sammle mehr auf deinen Trips!",
        PlaceError.LevelTooLow => "Dein Level ist noch zu niedrig — weiter aktiv bleiben!",
        PlaceError.AchievementLocked => "Dieses Gebäude schaltest du durch einen Erfolg frei.",
        PlaceError.MaxSizeReached => "Deine Stadt ist schon maximal gewachsen!",
        _ => string.Empty,
    };
}
