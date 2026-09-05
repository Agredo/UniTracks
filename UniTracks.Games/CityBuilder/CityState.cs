namespace UniTracks.Games.CityBuilder;

/// <summary>Complete state of the player's city: grid tiles plus the current coin balance.</summary>
public record CityState
{
    public const int DefaultGridSize = 6;

    public int GridSize { get; init; } = DefaultGridSize;

    /// <summary>All tiles, row-major (Y * GridSize + X). Always GridSize² entries.</summary>
    public IReadOnlyList<CityTile> Tiles { get; init; } = Array.Empty<CityTile>();

    /// <summary>Spendable coins (earned − spent).</summary>
    public int Coins { get; init; }

    /// <summary>Total coins ever earned through trips and achievements.</summary>
    public int CoinsEarned { get; init; }

    /// <summary>Total coins spent on buildings (minus demolition refunds).</summary>
    public int CoinsSpent { get; init; }

    public CityTile? GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= GridSize || y >= GridSize)
        {
            return null;
        }

        return Tiles.Count == GridSize * GridSize ? Tiles[y * GridSize + x] : null;
    }

    public int BuildingCount => Tiles.Count(t => !t.IsEmpty);
}
