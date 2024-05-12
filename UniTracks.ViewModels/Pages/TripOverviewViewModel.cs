using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Services.Navigation;

namespace UniTracks.ViewModels.Pages;

public partial class TripOverviewViewModel : ObservableObject
{
    public INavigation Navigation { get; }
    public INavigationRoutes NavigationRoutes { get; }

    [ObservableProperty]
    private Trip trip = new Trip();

    [ObservableProperty]
    private ObservableCollection<Location> locations = new ObservableCollection<Location>();

    public TripOverviewViewModel(INavigation navigation, INavigationRoutes navigationRoutes)
    {
        Navigation = navigation;
        NavigationRoutes = navigationRoutes;

        Navigation.Parameters.TryGetValue("parameter", out var parameter);

        Trip = parameter as Trip;

        if (Trip != null)
        {
            Trip.Locations.ForEach(location => Locations.Add(location));
        }
    }
}
