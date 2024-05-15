using Maui.BindableProperty.Generator.Core;
using System.Windows.Input;
using UniTracks.Models.Trip;

namespace UniTracks.Maui.Views.Tabs.StartPage;

public partial class TripsTab : ContentView
{
	public TripsTab()
	{
		InitializeComponent();
    }

	[AutoBindable(OnChanged = nameof(TripsChanged))]
    private ICollection<Trip> trips;

    [AutoBindable(OnChanged = nameof(SelectedTripChanged))]
    private Trip selectedTrip;

    [AutoBindable(OnChanged = nameof(SelectionChangedCommandChanged), DefaultBindingMode = nameof(BindingMode.TwoWay))]
    private ICommand selectionChaged;

    [AutoBindable(OnChanged = nameof(PullToRefreshCommandCommandChanged), DefaultBindingMode = nameof(BindingMode.TwoWay))]
    private ICommand pullToRefresh;

    [AutoBindable(OnChanged = nameof(IsRefreshingChanged), DefaultBindingMode = nameof(BindingMode.TwoWay))]
    private bool isRefreshing;

    private void TripsChanged(ICollection<Trip> newTrips)
    {
		TracksCollectionView.ItemsSource = newTrips;
    }

	private void SelectedTripChanged(Trip newTrip)
    {
		TracksCollectionView.SelectedItem = newTrip;
    }

    private void TracksCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
		SelectedTrip = e.CurrentSelection.FirstOrDefault() as Trip;
    }

	private void SelectionChangedCommandChanged(ICommand newCommand)
    {
        TracksCollectionView.SelectionChangedCommand = newCommand;
		
    }

    private void PullToRefreshCommandCommandChanged(ICommand newCommand)
    {
        Refresh.Command = newCommand;
    }

    private void IsRefreshingChanged(bool newvalue)
    {
        Refresh.IsRefreshing = newvalue; 
    }
}