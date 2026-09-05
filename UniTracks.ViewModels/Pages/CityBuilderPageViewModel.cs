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

        _ = LoadAsync();
    }

    /// <summary>Shop entries in catalog order (cheapest first), rebuilt with unlock state on every refresh.</summary>
    public ObservableCollection<ShopItemViewModel> ShopItems { get; } = new();

    [ObservableProperty]
    private CityState city = new();

    [ObservableProperty]
    private int coins;

    [ObservableProperty]
    private string coinsLabel = "0 🪙";

    [ObservableProperty]
    private string levelLabel = "Level 1";

    [ObservableProperty]
    private string nextLevelTeaser = string.Empty;

    /// <summary>Expansion call-to-action ("＋ Erweitern"), empty when maxed out.</summary>
    [ObservableProperty]
    private string expansionLabel = string.Empty;

    /// <summary>Whether the next expansion can be bought right now.</summary>
    [ObservableProperty]
    private bool canExpand;

    /// <summary>Whether any expansion step remains (controls button visibility).</summary>
    [ObservableProperty]
    private bool hasExpansion;

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
    private async Task SelectBuilding(ShopItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (!item.IsUnlocked)
        {
            await dialogService.AlertAsync("Gesperrt", item.LockLabel.TrimStart(), "OK");
            return;
        }

        // Tapping the active building again deselects it.
        SelectedBuilding = SelectedBuilding?.Id == item.Id ? null : item.Building;
    }

    [RelayCommand]
    private void ToggleDemolishMode()
    {
        IsDemolishMode = !IsDemolishMode;
    }

    [RelayCommand]
    private async Task Expand()
    {
        await ApplyResultAsync(await cityBuilderService.TryExpandAsync());
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
        LevelLabel = $"Level {city.Level}";

        int xpIntoLevel = city.Xp % 100;
        NextLevelTeaser = $"Noch {100 - xpIntoLevel} XP bis Level {city.Level + 1} (+{(city.Level + 1) * 100:N0} 🪙)";

        var step = city.NextExpansion;
        HasExpansion = step is not null;
        CanExpand = city.CanExpand;
        ExpansionLabel = step is null
            ? string.Empty
            : city.Level < step.RequiredLevel
                ? $"＋ {step.GridSize}×{step.GridSize} — 🔒 Level {step.RequiredLevel}"
                : $"＋ {step.GridSize}×{step.GridSize} ({step.Cost:N0} 🪙)";

        // Rebuild shop items with fresh unlock/affordability state.
        SelectedBuilding = null;
        ShopItems.Clear();
        foreach (var building in BuildingCatalog.Buildings)
        {
            ShopItems.Add(new ShopItemViewModel(building, city));
        }
    }
}
