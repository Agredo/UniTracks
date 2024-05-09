using System.Collections.ObjectModel;
using Location = UniTracks.Models.Location.Location;
using Mapsui.UI.Maui;
using Maui.BindableProperty.Generator.Core;
using Mapsui.Projections;

namespace UniTracks.Maui.Views.Controls;

public partial class MapView : ContentView
{
	public MapView()
	{
		InitializeComponent();
        ControlMapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
    }

    [AutoBindable(OnChanged = nameof(LocationChaged))]
    private Collection<Location> locations = new Collection<Models.Location.Location>();

    private void LocationChaged(Collection<Location> newLocartions)
    {
        ControlMapView.Pins.Clear();
        foreach (var location in newLocartions)
        {
            var smc = SphericalMercator.FromLonLat(location.Latitude, location.Longitude);
            Position position = new Position(location.Longitude, location.Latitude);
            var pin = new Pin(ControlMapView) { Label = "Test", Position = position };
            ControlMapView.Pins.Add(pin);
        }
    }
}