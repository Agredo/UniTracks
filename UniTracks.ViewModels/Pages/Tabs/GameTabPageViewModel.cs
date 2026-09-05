using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Games.Catalog;
using UniTracks.Services.Game;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class GameTabPageViewModel : ObservableObject
{
    private readonly IGameCatalogService gameCatalogService;

    public INavigationService Navigation { get; }

    public ObservableCollection<GameInfo> Games { get; } = new();

    [ObservableProperty]
    private int coins;

    [ObservableProperty]
    private string coinsLabel = "0 🪙";

    public GameTabPageViewModel(IGameCatalogService gameCatalogService, INavigationService navigation)
    {
        this.gameCatalogService = gameCatalogService;
        Navigation = navigation;

        foreach (var game in gameCatalogService.GetGames())
        {
            Games.Add(game);
        }

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task OpenGame(GameInfo? game)
    {
        if (game is null || !game.IsAvailable)
        {
            return;
        }

        await Navigation.ShellNavigationTo(game.Route, new Dictionary<string, object>());
    }

    private async Task RefreshAsync()
    {
        Coins = await gameCatalogService.GetCoinBalanceAsync();
        CoinsLabel = $"{Coins:N0} 🪙";
    }
}
