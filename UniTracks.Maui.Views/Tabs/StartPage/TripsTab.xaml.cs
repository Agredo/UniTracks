using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Maui;
using UniTracks.Models.Trip;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Tabs.StartPage;

public partial class TripsTab : ContentView
{
    public TripsTab()
    {
        InitializeComponent();
        IsRefreshing = Refresh.IsRefreshing;

        Refresh.Refreshing += (s, e) =>
        {
            IsRefreshing = true;
        };
    }

    [BindableProperty(PropertyChangedMethodName = nameof(OnTripsPropertyChanged))]
    public partial ICollection<Trip>? Trips { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectedTripPropertyChanged), DefaultBindingMode = BindingMode.TwoWay)]
    public partial Trip? SelectedTrip { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionChangedPropertyChanged), DefaultBindingMode = BindingMode.TwoWay)]
    public partial ICommand? SelectionChanged { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnPullToRefreshPropertyChanged), DefaultBindingMode = BindingMode.TwoWay)]
    public partial ICommand? PullToRefresh { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnLoadMorePropertyChanged))]
    public partial ICommand? LoadMore { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnHasMoreTripsPropertyChanged))]
    public partial bool HasMoreTrips { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnIsRefreshingPropertyChanged), DefaultBindingMode = BindingMode.TwoWay)]
    public partial bool IsRefreshing { get; set; }

    [BindableProperty]
    public partial bool CompactLayout { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnOpenFiltersPropertyChanged))]
    public partial ICommand? OpenFilters { get; set; }

    [BindableProperty]
    public partial ICommand? ResetFilters { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnActiveFilterCountPropertyChanged))]
    public partial int ActiveFilterCount { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnHasActiveFiltersPropertyChanged))]
    public partial bool HasActiveFilters { get; set; }

    private static void OnTripsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.TripsChanged((ICollection<Trip>?)newValue);
    }

    private static void OnSelectedTripPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.SelectedTripChanged((Trip?)newValue);
    }

    private static void OnSelectionChangedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.SelectionChangedCommandChanged((ICommand?)newValue);
    }

    private static void OnPullToRefreshPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.PullToRefreshCommandChanged((ICommand?)newValue);
    }

    private static void OnIsRefreshingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.IsRefreshingChanged((bool)newValue);
    }

    private static void OnLoadMorePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.LoadMoreCommandChanged((ICommand?)newValue);
    }

    private static void OnHasMoreTripsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.LoadMoreButton.IsVisible = (bool)newValue;
    }

    private static void OnOpenFiltersPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.FilterButton.Command = (ICommand?)newValue;
    }

    private static void OnActiveFilterCountPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        var count = (int)newValue;

        tab.FilterBadgeLabel.Text = count.ToString();
        tab.FilterBadge.IsVisible = count > 0;
    }

    private static void OnHasActiveFiltersPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tab = (TripsTab)bindable;
        tab.ActiveFiltersChanged((bool)newValue);
    }

    private void TripsChanged(ICollection<Trip>? newTrips)
    {
        TracksCollectionView.ItemsSource = newTrips;
    }

    /// <summary>
    /// An active filter is worth a visible state on the button: without it the list looks empty for
    /// no apparent reason and the way back is hard to find.
    /// </summary>
    private void ActiveFiltersChanged(bool hasActiveFilters)
    {
        if (hasActiveFilters)
        {
            FilterButton.BackgroundColor = ResourceColor("AccentSoft");
            FilterButton.BorderColor = ResourceColor("Accent");
            FilterButton.TextColor = ResourceColor("Accent");
        }
        else
        {
            FilterButton.BackgroundColor = ResourceColor("CardAlt");
            FilterButton.BorderColor = ResourceColor("CardStroke");
            FilterButton.TextColor = ResourceColor("TextPrimary");
        }

        // "Nothing recorded yet" and "nothing matches the filter" need different words, so the empty
        // view switches between them instead of guessing one.
        EmptyNoTrips.IsVisible = !hasActiveFilters;
        EmptyFilteredTrips.IsVisible = hasActiveFilters;
    }

    private static Color ResourceColor(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Colors.Transparent;

    private void OnFilterClicked(object? sender, EventArgs e) => Execute(OpenFilters);

    private void OnResetFiltersClicked(object? sender, EventArgs e) => Execute(ResetFilters);

    private static void Execute(ICommand? command)
    {
        if (command is { } executable && executable.CanExecute(null))
        {
            executable.Execute(null);
        }
    }

    private void SelectedTripChanged(Trip? newTrip)
    {
        TracksCollectionView.SelectedItem = newTrip;
    }

    private void TracksCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedTrip = e.CurrentSelection.FirstOrDefault() as Trip;

        // MAUI keeps SelectedItem set after navigation, so tapping the same trip again
        // would not re-trigger SelectionChanged. Reset the selection to allow re-selection.
        TracksCollectionView.SelectedItem = null;
    }

    private void SelectionChangedCommandChanged(ICommand? newCommand)
    {
        TracksCollectionView.SelectionChangedCommand = newCommand;
    }

    private void PullToRefreshCommandChanged(ICommand? newCommand)
    {
        Refresh.Command = newCommand;
    }

    private void LoadMoreCommandChanged(ICommand? newCommand)
    {
        TracksCollectionView.RemainingItemsThresholdReachedCommand = newCommand;
    }

    private void OnLoadMoreClicked(object? sender, EventArgs e)
    {
        if (LoadMore is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private void IsRefreshingChanged(bool newValue)
    {
        Refresh.IsRefreshing = newValue;
    }

    private TripTabPageViewModel? ViewModel => BindingContext as TripTabPageViewModel;

    private async void OnRenameInvoked(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem { } item || item.BindingContext is not Trip trip)
        {
            return;
        }

        var newName = await Shell.Current.DisplayPromptAsync(
            "Trip umbenennen",
            "Neuer Name:",
            "Speichern",
            "Abbrechen",
            initialValue: trip.Name ?? string.Empty);

        if (string.IsNullOrWhiteSpace(newName) || newName.Trim() == trip.Name)
        {
            return;
        }

        if (ViewModel is { } vm)
        {
            await vm.RenameTripAsync(trip, newName.Trim());
        }
    }

    private async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem { } item || item.BindingContext is not Trip trip)
        {
            return;
        }

        var title = string.IsNullOrWhiteSpace(trip.Name) ? trip.StartTime.LocalDateTime.ToString("dd.MM.yyyy HH:mm") : trip.Name;
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Trip löschen",
            $"Möchtest du „{title}“ wirklich löschen?",
            "Löschen",
            "Abbrechen");

        if (confirm && ViewModel is { } vm)
        {
            await vm.DeleteTripAsync(trip);
        }
    }
}