using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.User;
using UniTracks.Services.Stats;

namespace UniTracks.ViewModels.Pages.Tabs;

/// <summary>
/// Der Profil-Tab: der Kopfbereich zeigt den Fortschritt (Level, Streak, Gesamtdistanz) und die
/// Kennzahlen der laufenden Woche, darunter führen Karten auf die Profilbearbeitung, die Statistiken
/// und die Einstellungen. Alles kommt aus einem einzigen
/// <see cref="IStatisticsService.GetSnapshotAsync"/>-Aufruf, der die Trips einmal auswertet.
/// </summary>
public partial class UserPagevViewModel : ObservableObject
{
    /// <summary>Same formatting as the statistics page: a German UI shows "12,3", not "12.3".</summary>
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public INavigationService Navigation { get; }

    [ObservableProperty]
    private string levelText = "-";

    [ObservableProperty]
    private string streakText = "-";

    [ObservableProperty]
    private string totalDistanceText = "-";

    [ObservableProperty]
    private string weekDistanceText = "-";

    [ObservableProperty]
    private string tripsText = "-";

    [ObservableProperty]
    private string yearDistanceText = "-";

    /// <summary>Untertitel des Kopfbereichs; zeigt den Namen, sobald eines im Profil steht.</summary>
    [ObservableProperty]
    private string greetingText = "Bereit für den nächsten Trip";

    private const string DefaultGreeting = "Bereit für den nächsten Trip";

    private readonly IStatisticsService statisticsService;
    private readonly IRepository repository;

    public UserPagevViewModel(
        INavigationService navigation,
        IStatisticsService statisticsService,
        IRepository repository)
    {
        Navigation = navigation;
        this.statisticsService = statisticsService;
        this.repository = repository;
    }

    /// <summary>
    /// Loads the numbers in the hero. Called from the page's <c>OnAppearing</c>: a finished trip, a
    /// new day or an edited profile changes them while the tab is off screen.
    /// </summary>
    public async Task RefreshAsync()
    {
        var snapshot = await statisticsService.GetSnapshotAsync(weekCount: 2);

        LevelText = snapshot.Level.ToString(GermanCulture);
        StreakText = snapshot.CurrentStreakDays.ToString(GermanCulture);
        TotalDistanceText = snapshot.TotalDistanceKm.ToString("0.0", GermanCulture);
        WeekDistanceText = snapshot.WeekDistanceKm.ToString("0.0", GermanCulture);
        TripsText = snapshot.TotalTrips.ToString(GermanCulture);
        YearDistanceText = snapshot.YearDistanceKm.ToString("0.0", GermanCulture);

        GreetingText = await GetGreetingAsync();
    }

    /// <summary>Greets with the profile name; the page can be opened before a profile exists.</summary>
    private async Task<string> GetGreetingAsync()
    {
        try
        {
            var users = (await repository.GetAllAsync<User>()).ToList();
            var name = users.FirstOrDefault()?.Name;

            return string.IsNullOrWhiteSpace(name)
                ? DefaultGreeting
                : $"Hallo {name.Trim()}";
        }
        catch (Exception)
        {
            // The hero must render even if the profile cannot be read.
            return DefaultGreeting;
        }
    }

    [RelayCommand]
    private async Task OpenProfile()
    {
        await Navigation.ShellNavigationTo("ProfilePage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenStatistics()
    {
        await Navigation.ShellNavigationTo("StatisticsPage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenAbout()
    {
        await Navigation.ShellNavigationTo("AboutPage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenFeedback()
    {
        await Navigation.ShellNavigationTo("FeedbackPage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenHelp()
    {
        await Navigation.ShellNavigationTo("HelpPage", new Dictionary<string, object>());
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        await Navigation.ShellNavigationTo("SettingsPage", new Dictionary<string, object>());
    }
}
