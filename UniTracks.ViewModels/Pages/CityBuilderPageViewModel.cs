using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Games.CityBuilder;
using UniTracks.Services.Game;

namespace UniTracks.ViewModels.Pages;

public partial class CityBuilderPageViewModel : ObservableObject
{
    private readonly ICityBuilderService cityBuilderService;
    private readonly IDialogService dialogService;

    public CityBuilderPageViewModel(ICityBuilderService cityBuilderService, IDialogService dialogService)
    {
        this.cityBuilderService = cityBuilderService;
        this.dialogService = dialogService;

        foreach (var building in BuildingCatalog.Buildings)
        {
            ShopItems.Add(building);
        }

        _ = LoadAsync();
    }

    /// <summary>Shop entries in catalog order (cheapest first).</summary>
    public ObservableCollection<BuildingDefinition> ShopItems { get; } = new();

    [ObservableProperty]
    private CityState city = new();

    [ObservableProperty]
    private int coins;

    [ObservableProperty]
    private string coinsLabel = "0 🪙";

    [ObservableProperty]
    private BuildingDefinition? selectedBuilding;

    [ObservableProperty]
    private bool isDemolishMode;

    [ObservableProperty]
    private string modeHint = "Wähle ein Gebäude und tippe auf ein Feld.";

    private CityTile? lastTappedTile;

    partial void OnSelectedBuildingChanged(BuildingDefinition? value)
    {
        if (value is not null)
        {
            IsDemolishMode = false;
            ModeHint = $"{value.Icon} {value.Name} ({value.Cost} 🪙) — tippe auf ein freies Feld.";

            // Comfort flow: tile tapped first, building picked afterwards → place right away.
            if (lastTappedTile is { } tile)
            {
                _ = PlaceAsync(value, tile);
            }
        }
        else if (!IsDemolishMode)
        {
            ModeHint = "Wähle ein Gebäude und tippe auf ein Feld.";
        }
    }

    partial void OnIsDemolishModeChanged(bool value)
    {
        if (value)
        {
            SelectedBuilding = null;
            ModeHint = "🧨 Abriss-Modus: tippe auf ein Gebäude (50 % Rückerstattung).";
        }
        else if (SelectedBuilding is null)
        {
            ModeHint = "Wähle ein Gebäude und tippe auf ein Feld.";
        }
    }

    [RelayCommand]
    private void SelectBuilding(BuildingDefinition? building)
    {
        // Tapping the active building again deselects it.
        SelectedBuilding = SelectedBuilding?.Id == building?.Id ? null : building;
    }

    [RelayCommand]
    private void ToggleDemolishMode()
    {
        IsDemolishMode = !IsDemolishMode;
    }

    [RelayCommand]
    private async Task TileTapped(CityTile? tile)
    {
        if (tile is null)
        {
            return;
        }

        lastTappedTile = tile;

        if (IsDemolishMode)
        {
            await ApplyResultAsync(await cityBuilderService.TryDemolishAsync(tile.X, tile.Y));
        }
        else if (SelectedBuilding is not null)
        {
            await ApplyResultAsync(await cityBuilderService.TryPlaceAsync(SelectedBuilding.Id, tile.X, tile.Y));
        }
        // Nothing selected: pure selection tap — the tile is remembered for the comfort flow above.
    }

    private async Task PlaceAsync(BuildingDefinition building, CityTile tile)
    {
        await ApplyResultAsync(await cityBuilderService.TryPlaceAsync(building.Id, tile.X, tile.Y));
    }

    private async Task ApplyResultAsync(PlaceResult result)
    {
        if (!result.Success)
        {
            await dialogService.AlertAsync("Hinweis", result.ErrorMessage, "OK");
            return;
        }

        ApplyCity(result.City!);
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ApplyCity(await cityBuilderService.GetCityAsync());
    }

    private void ApplyCity(CityState city)
    {
        City = city;
        Coins = city.Coins;
        CoinsLabel = $"{Coins:N0} 🪙";
    }
}
