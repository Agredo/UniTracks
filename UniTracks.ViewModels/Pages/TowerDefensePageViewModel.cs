using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Games.TowerDefense;
using UniTracks.Services.Game;

namespace UniTracks.ViewModels.Pages;

public partial class TowerDefensePageViewModel : ObservableObject
{
    private readonly ITowerDefenseService towerDefenseService;
    private readonly IDialogService dialogService;

    /// <summary>Guards against saving the same finished run twice.</summary>
    private bool runSaved;

    public TowerDefensePageViewModel(ITowerDefenseService towerDefenseService, IDialogService dialogService)
    {
        this.towerDefenseService = towerDefenseService;
        this.dialogService = dialogService;

        _ = LoadAsync();
    }

    /// <summary>Shop entries in catalog order (cheapest first), rebuilt with unlock state on every profile refresh.</summary>
    public ObservableCollection<TowerShopItemViewModel> ShopItems { get; } = new();

    /// <summary>The active run. Mutated in place by <see cref="DefenseEngine.Tick"/>; replaced on restart.</summary>
    [ObservableProperty]
    private DefenseState state = DefenseEngine.NewRun(Array.Empty<string>());

    [ObservableProperty]
    private DefenseProfile profile = new();

    [ObservableProperty]
    private TowerDefinition? selectedTower;

    [ObservableProperty]
    private bool isSellMode;

    [ObservableProperty]
    private string coinsLabel = "0 🪙";

    [ObservableProperty]
    private string energyLabel = "0 ⚡";

    [ObservableProperty]
    private string livesLabel = "20 ❤️";

    [ObservableProperty]
    private string waveLabel = "Welle 1";

    [ObservableProperty]
    private string bestLabel = string.Empty;

    /// <summary>Whether the next wave can be started right now (between waves).</summary>
    [ObservableProperty]
    private bool canStartWave = true;

    /// <summary>Whether the run is over (drives the game-over overlay).</summary>
    [ObservableProperty]
    private bool isLost;

    /// <summary>Summary line shown on the game-over overlay.</summary>
    [ObservableProperty]
    private string resultLabel = string.Empty;

    [ObservableProperty]
    private string modeHint = "Wähle einen Turm und tippe auf ein freies Feld.";

    partial void OnSelectedTowerChanged(TowerDefinition? value)
    {
        if (value is not null)
        {
            IsSellMode = false;
            ModeHint = $"{value.Icon} {value.Name} ({value.EnergyCost} ⚡) — tippe auf ein freies Feld.";
        }
        else if (!IsSellMode)
        {
            ModeHint = "Wähle einen Turm und tippe auf ein freies Feld.";
        }
    }

    partial void OnIsSellModeChanged(bool value)
    {
        if (value)
        {
            SelectedTower = null;
            ModeHint = "💥 Verkaufen: tippe auf einen Turm (50 % Rückerstattung).";
        }
        else if (SelectedTower is null)
        {
            ModeHint = "Wähle einen Turm und tippe auf ein freies Feld.";
        }
    }

    [RelayCommand]
    private async Task SelectTower(TowerShopItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (!item.IsUnlocked)
        {
            await UnlockAsync(item);
            return;
        }

        // Tapping the active tower again deselects it.
        SelectedTower = SelectedTower?.Id == item.Id ? null : item.Tower;
    }

    [RelayCommand]
    private void ToggleSellMode()
    {
        IsSellMode = !IsSellMode;
    }

    [RelayCommand]
    private async Task TileTapped(DefenseTile? tile)
    {
        if (tile is null || IsLost)
        {
            return;
        }

        DefenseResult result = IsSellMode
            ? DefenseEngine.SellTower(State, tile.X, tile.Y)
            : SelectedTower is not null
                ? DefenseEngine.PlaceTower(State, SelectedTower.Id, tile.X, tile.Y)
                : DefenseResult.Fail(DefenseError.TileEmpty);

        if (!result.Success && result.Error != DefenseError.TileEmpty)
        {
            await dialogService.AlertAsync("Hinweis", result.ErrorMessage, "OK");
        }

        RefreshLabels();
    }

    [RelayCommand]
    private void StartWave()
    {
        DefenseEngine.StartWave(State);
        RefreshLabels();
    }

    /// <summary>Advances the simulation — wired to the map view's animation timer.</summary>
    [RelayCommand]
    private async Task Tick(int elapsedMs)
    {
        DefenseEngine.Tick(State, elapsedMs);
        RefreshLabels();

        if (State.Phase == DefensePhase.Lost && !runSaved)
        {
            runSaved = true;
            ApplyProfile(await towerDefenseService.SaveRunResultAsync(State.ClearedWave, State.Score));
        }
    }

    [RelayCommand]
    private void Restart()
    {
        State = DefenseEngine.NewRun(Profile.UnlockedTowerIds);
        runSaved = false;
        IsLost = false;
        SelectedTower = null;
        IsSellMode = false;
        RefreshLabels();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    private async Task UnlockAsync(TowerShopItemViewModel item)
    {
        var result = await towerDefenseService.TryUnlockAsync(item.Id);
        if (!result.Success)
        {
            await dialogService.AlertAsync("Gesperrt", result.ErrorMessage, "OK");
            return;
        }

        ApplyProfile(await towerDefenseService.GetProfileAsync());
        SelectedTower = item.Tower;
    }

    private async Task LoadAsync()
    {
        var profile = await towerDefenseService.GetProfileAsync();
        ApplyProfile(profile);

        // First load starts the run with the player's unlocked towers.
        State = DefenseEngine.NewRun(profile.UnlockedTowerIds);
        runSaved = false;
        IsLost = false;
        RefreshLabels();
    }

    private void ApplyProfile(DefenseProfile profile)
    {
        Profile = profile;
        CoinsLabel = $"{profile.Coins:N0} 🪙";
        BestLabel = profile.BestWave > 0
            ? $"Rekord: Welle {profile.BestWave} · {profile.BestScore:N0} Punkte"
            : "Noch kein Rekord — starte deine erste Welle!";

        SelectedTower = null;
        ShopItems.Clear();
        foreach (var tower in TowerCatalog.Towers)
        {
            ShopItems.Add(new TowerShopItemViewModel(tower, profile));
        }
    }

    private void RefreshLabels()
    {
        EnergyLabel = $"{State.Energy:N0} ⚡";
        LivesLabel = $"{State.Lives} ❤️";
        WaveLabel = $"Welle {State.NextWave}";
        CanStartWave = State.Phase == DefensePhase.Building;
        IsLost = State.Phase == DefensePhase.Lost;
        if (IsLost)
        {
            ResultLabel = $"Geschaffte Wellen: {State.ClearedWave} · Punkte: {State.Score:N0}";
        }
    }
}
