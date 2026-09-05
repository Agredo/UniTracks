using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Models.Achievement;
using UniTracks.Services.Stats;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class AchievementsPageViewModel : ObservableObject
{
    private readonly IGamificationService gamificationService;

    public AchievementsPageViewModel(IGamificationService gamificationService)
    {
        this.gamificationService = gamificationService;
        _ = LoadAsync();
    }

    [ObservableProperty]
    private int level = 1;

    [ObservableProperty]
    private int xp;

    [ObservableProperty]
    private double levelProgressFraction;

    [ObservableProperty]
    private string levelLabel = "Level 1";

    [ObservableProperty]
    private string xpLabel = "0 XP";

    [ObservableProperty]
    private int bestStreakDays;

    [ObservableProperty]
    private int currentStreakDays;

    [ObservableProperty]
    private double totalDistanceKm;

    [ObservableProperty]
    private int totalTrips;

    public ObservableCollection<Achievement> Achievements { get; } = new();

    private async Task LoadAsync()
    {
        var stats = await gamificationService.ComputeAsync();

        Level = stats.Level;
        Xp = stats.Xp;
        LevelLabel = stats.LevelLabel;
        XpLabel = stats.XpLabel;
        LevelProgressFraction = stats.LevelProgressFraction;
        BestStreakDays = stats.BestStreakDays;
        CurrentStreakDays = stats.CurrentStreakDays;
        TotalDistanceKm = stats.TotalDistanceKm;
        TotalTrips = stats.TotalTrips;

        Achievements.Clear();
        foreach (var achievement in stats.Achievements)
        {
            Achievements.Add(achievement);
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }
}
