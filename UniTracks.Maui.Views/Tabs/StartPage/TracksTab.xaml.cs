using Maui.BindableProperty.Generator.Core;
using UniTracks.Models.Trip;

namespace UniTracks.Maui.Views.Tabs.StartPage;

public partial class TracksTab : ContentView
{
	public TracksTab()
	{
		InitializeComponent();
	}

	[AutoBindable(OnChanged = nameof(TripsChanged))]
    private ICollection<Trip> trips;

	private void TripsChanged(ICollection<Trip> newTrips)
    {
		TracksCollectionView.ItemsSource = newTrips;
    }
}