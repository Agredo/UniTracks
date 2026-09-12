using UniTracks.Games.Shared.Economy;

namespace UniTracks.Services.Game;

/// <summary>
/// Supplies the shared coin account. Every game reads its balance from here, so the city
/// builder and the tower defense game can never disagree about how many coins are left.
/// </summary>
public interface ICoinAccountService
{
    Task<CoinAccount> GetAsync();
}
