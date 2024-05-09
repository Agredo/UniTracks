using System.Collections.ObjectModel;
using Location = UniTracks.Models.Location.Location;
using Mapsui.UI.Maui;
using Maui.BindableProperty.Generator.Core;

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
        ControlMapView.Drawables.Clear();

        var line = new Mapsui.UI.Maui.Polyline
        {
            StrokeWidth = 3, 
            StrokeColor = Mapsui.UI.Maui.KnownColor.Red, 
            IsClickable = true, 
        };

        foreach (var location in newLocartions)
        {
            var position = new Position(location.Longitude, location.Latitude);
            line.Positions.Add(position);
        }

        ControlMapView.Drawables.Add(line);

        //Add Start and End Pin
        if (newLocartions.Count > 0)
        {
            var startPin = new Mapsui.UI.Maui.Pin(ControlMapView)
            {
                Label = "Start",
                Type = PinType.Svg,
                Position = new Position(newLocartions[0].Longitude, newLocartions[0].Latitude),
                Scale = 0.5F,
                Color = Colors.Green,
            };

            var endPin = new Mapsui.UI.Maui.Pin(ControlMapView)
            {
                Label = "End",
                Type = PinType.Svg,
                Position = new Position(newLocartions[^1].Longitude, newLocartions[^1].Latitude),
                Scale = 0.5F,
                Color = Colors.Red
            };

            SetPinCallout(startPin, "Start", 10, 14, Colors.Black, Colors.Black);
            SetPinCallout(endPin, "Finish", 10, 14, Colors.Black, Colors.Black);

            ControlMapView.Pins.Add(startPin);
            ControlMapView.Pins.Add(endPin);
        }

    }
    private void SetPinCallout(Mapsui.UI.Maui.Pin pin, string title, int arrowHeight, int titleFontSize, Color color, Color titleFontColor)
    {
        pin.ShowCallout();
        pin.Callout.ArrowHeight = arrowHeight;
        pin.Callout.TitleFontSize = titleFontSize;
        pin.Callout.Color = color;
        pin.Callout.TitleFontColor = titleFontColor;
        pin.Callout.Title = title;
        pin.Callout.IsClosableByClick = false;
    }
}