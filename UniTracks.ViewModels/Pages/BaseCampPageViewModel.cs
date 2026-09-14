using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Games.BaseCamp;
using UniTracks.Services.Game;

namespace UniTracks.ViewModels.Pages;

public partial class BaseCampPageViewModel : ObservableObject
{
    private readonly IBaseCampService baseCampService;
    private readonly IDialogService dialogService;

    public BaseCampPageViewModel(IBaseCampService baseCampService, IDialogService dialogService)
    {
        this.baseCampService = baseCampService;
        this.dialogService = dialogService;

        _ = LoadAsync();
    }

    /// <summary>Shop entries in catalog order, rebuilt with level/unlock state on every refresh.</summary>
    public ObservableCollection<BaseCampShopItemViewModel> ModuleItems { get; } = new();

    [ObservableProperty]
    private CampState camp = new();

    [ObservableProperty]
    private int supplies;

    [ObservableProperty]
    private string suppliesLabel = "0 📦";

    [ObservableProperty]
    private string capacityLabel = string.Empty;

    /// <summary>Fill level of the stock in 0…1, drives the progress bar.</summary>
    [ObservableProperty]
    private double fillRatio;

    [ObservableProperty]
    private string rateLabel = string.Empty;

    [ObservableProperty]
    private string coinsLabel = "0 🪙";

    [ObservableProperty]
    private string levelLabel = "Level 1";

    [ObservableProperty]
    private string totalCollectedLabel = string.Empty;

    /// <summary>Streak, freshness and weekly kilometres in one line.</summary>
    [ObservableProperty]
    private string statusHint = string.Empty;

    /// <summary>Hint why the camp is producing slowly ("" while everything is fine).</summary>
    [ObservableProperty]
    private string freshnessHint = string.Empty;

    /// <summary>Whether a freshness warning is currently shown.</summary>
    [ObservableProperty]
    private bool hasFreshnessHint;

    /// <summary>Caption of the harvest button, including the amount waiting.</summary>
    [ObservableProperty]
    private string collectLabel = "Sammeln";

    /// <summary>Whether anything is waiting in the camp right now.</summary>
    [ObservableProperty]
    private bool canCollect;

    /// <summary>Whether the camp is at its capacity — the rest of the day produces nothing.</summary>
    [ObservableProperty]
    private bool isFull;

    [RelayCommand]
    private async Task Collect()
    {
        await ApplyResultAsync(await baseCampService.CollectAsync());
    }

    [RelayCommand]
    private async Task Upgrade(BaseCampShopItemViewModel? item)
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

        await ApplyResultAsync(await baseCampService.TryUpgradeAsync(item.Id));
    }

    /// <summary>Tapping a module in the scene is a shortcut for its shop entry.</summary>
    [RelayCommand]
    private async Task ModuleTapped(string? moduleId)
    {
        if (moduleId is null)
        {
            return;
        }

        await Upgrade(ModuleItems.FirstOrDefault(i => i.Id == moduleId));
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ApplyCamp(await baseCampService.GetCampAsync());
    }

    private async Task ApplyResultAsync(CollectResult result)
    {
        if (!result.Success)
        {
            await dialogService.AlertAsync("Hinweis", result.ErrorMessage, "OK");
            return;
        }

        ApplyCamp(result.Camp!);
    }

    private async Task ApplyResultAsync(UpgradeResult result)
    {
        if (!result.Success)
        {
            await dialogService.AlertAsync("Hinweis", result.ErrorMessage, "OK");
            return;
        }

        await LoadAsync();
    }

    private void ApplyCamp(CampState camp)
    {
        Camp = camp;
        Supplies = camp.Supplies;
        SuppliesLabel = $"{camp.Supplies:N0} 📦";
        CapacityLabel = $"Lager: {camp.Stock:N0} / {camp.Capacity:N0} 📦";
        FillRatio = camp.FillRatio;
        RateLabel = $"+{camp.RatePerHour:N1} 📦/h";
        CoinsLabel = $"{camp.Coins:N0} 🪙";
        LevelLabel = $"Level {camp.Level}";
        TotalCollectedLabel = $"Insgesamt gesammelt: {camp.TotalCollected:N0} 📦";

        StatusHint = camp.WeightedWeeklyKm <= 0
            ? "Noch keine Kilometer diese Woche — das Lager füllt sich nur langsam."
            : $"{camp.WeightedWeeklyKm:N1} km diese Woche · Streak {camp.CurrentStreakDays} Tage (×{camp.StreakFactor:N2})";

        // Freshness only drops below 1 once the player stopped moving for a day.
        FreshnessHint = camp.Freshness >= 0.999
            ? string.Empty
            : camp.Freshness >= 0.5
                ? $"⏸️ Etwas eingerostet — nur {camp.Freshness:P0} Leistung. Eine Tour bringt dich zurück."
                : $"🥶 Lange Pause — nur {camp.Freshness:P0} Leistung. Eine kurze Runde weckt das Lager.";
        HasFreshnessHint = FreshnessHint.Length > 0;

        IsFull = camp.IsFull;
        CanCollect = camp.Stock > 0;
        CollectLabel = camp.Stock > 0 ? $"Sammeln ({camp.Stock:N0} 📦)" : "Sammeln";

        // Rebuild the shop with fresh level/unlock/affordability state.
        ModuleItems.Clear();
        foreach (var module in CampCatalog.Modules)
        {
            ModuleItems.Add(new BaseCampShopItemViewModel(module, camp));
        }
    }
}
