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

    /// <summary>A previously persisted run that the player may resume from the map-selection overlay.</summary>
    private DefenseState? resumeRun;

    public TowerDefensePageViewModel(ITowerDefenseService towerDefenseService, IDialogService dialogService)
    {
        this.towerDefenseService = towerDefenseService;
        this.dialogService = dialogService;

        _ = LoadAsync();
    }

    /// <summary>Shop entries in catalog order (cheapest first), rebuilt with unlock state on every profile refresh.</summary>
    public ObservableCollection<TowerShopItemViewModel> ShopItems { get; } = new();

    /// <summary>The selectable maps, easiest to hardest (see <see cref="MapCatalog"/>).</summary>
    public ObservableCollection<DefenseMap> Maps { get; } = new(MapCatalog.All);

    /// <summary>The map chosen for the current (or next) run.</summary>
    [ObservableProperty]
    private DefenseMap selectedMap;

    /// <summary>Whether the map-selection overlay is shown at the start of a run.</summary>
    [ObservableProperty]
    private bool isChoosingMap = true;

    /// <summary>Whether there is a persisted run that the player can resume from the selection overlay.</summary>
    [ObservableProperty]
    private bool canResume;

    /// <summary>Label for the resume button in the selection overlay.</summary>
    [ObservableProperty]
    private string resumeLabel = string.Empty;

    /// <summary>The active run. Mutated in place by <see cref="DefenseEngine.Tick"/>; replaced on restart.</summary>
    [ObservableProperty]
    private DefenseState state = DefenseEngine.NewRun(Array.Empty<string>(), MapCatalog.Default, 0, 0);

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
        if (tile is null || IsLost || IsChoosingMap)
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

        if (result.Success)
        {
            // Keep the tower layout so it survives an app restart.
            await towerDefenseService.SaveRunAsync(State);
        }
    }

    [RelayCommand]
    private void StartWave()
    {
        if (IsChoosingMap)
        {
            return;
        }

        DefenseEngine.StartWave(State);
        RefreshLabels();
    }

    /// <summary>Advances the simulation — wired to the map view's animation timer.</summary>
    [RelayCommand]
    private async Task Tick(int elapsedMs)
    {
        if (IsChoosingMap)
        {
            return;
        }

        bool wasRunning = State.Phase == DefensePhase.WaveRunning;
        DefenseEngine.Tick(State, elapsedMs);
        RefreshLabels();

        if (State.Phase == DefensePhase.Lost && !runSaved)
        {
            runSaved = true;
            ApplyProfile(await towerDefenseService.SaveRunResultAsync(State.ClearedWave, State.Score));

            // Keep the failed layout + wave so the player can retry next time with fresh energy.
            await towerDefenseService.SaveRunAsync(State);
        }
        else if (wasRunning && State.Phase == DefensePhase.Building)
        {
            // Wave cleared — persist the new wave + energy so the player resumes exactly here.
            await towerDefenseService.SaveRunAsync(State);
        }
    }

    [RelayCommand]
    private async Task Restart()
    {
        // Let the player pick a map again before starting the next run.
        IsChoosingMap = true;
        runSaved = false;
        IsLost = false;
        SelectedTower = null;
        IsSellMode = false;

        // Drop the stored progress so the run cannot be resumed mid-game-over.
        await towerDefenseService.ClearRunAsync();
        resumeRun = null;
        CanResume = false;
        ResumeLabel = string.Empty;
        SelectedMap = MapCatalog.Default;
        State = DefenseEngine.NewRun(Profile.UnlockedTowerIds, MapCatalog.Default, Profile.StartingEnergy, Profile.ClearBonus);
    }

    /// <summary>Resumes the persisted run (towers + wave + map) chosen from the selection overlay.</summary>
    [RelayCommand]
    private void ResumeMap()
    {
        if (resumeRun is null)
        {
            return;
        }

        State = resumeRun;
        SelectedMap = resumeRun.Map;
        IsChoosingMap = false;
        runSaved = false;
        IsLost = false;
        SelectedTower = null;
        IsSellMode = false;
        RefreshLabels();
    }

    /// <summary>Starts a fresh run on the chosen map and stores it as the current run.</summary>
    [RelayCommand]
    private async Task SelectMap(DefenseMap? map)
    {
        if (map is null)
        {
            return;
        }

        SelectedMap = map;
        IsChoosingMap = false;
        State = DefenseEngine.NewRun(Profile.UnlockedTowerIds, map, Profile.StartingEnergy, Profile.ClearBonus);
        runSaved = false;
        IsLost = false;
        SelectedTower = null;
        IsSellMode = false;
        RefreshLabels();

        // Persist the fresh run so a restart resumes exactly here.
        await towerDefenseService.SaveRunAsync(State);
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

        // Always show the map-selection overlay on entry so the player picks a trail. If a
        // persisted run exists (towers + wave + map), offer it as a "resume" option; energy
        // is freshly recomputed from activity when the player resumes.
        var savedRun = await towerDefenseService.LoadRunAsync();
        resumeRun = savedRun;
        CanResume = savedRun is not null;
        ResumeLabel = savedRun is null
            ? string.Empty
            : $"▶ Welle {savedRun.NextWave} auf {savedRun.Map.Name} fortsetzen";

        SelectedMap = savedRun?.Map ?? MapCatalog.Default;
        State = savedRun
            ?? DefenseEngine.NewRun(profile.UnlockedTowerIds, MapCatalog.Default, profile.StartingEnergy, profile.ClearBonus);
        IsChoosingMap = true;

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
