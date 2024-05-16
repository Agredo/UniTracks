using System.Collections.ObjectModel;
using Location = UniTracks.Models.Location.Location;
using Mapsui.UI.Maui;
using Maui.BindableProperty.Generator.Core;
using Mapsui;

namespace UniTracks.Maui.Views.Controls;

public partial class MapView : ContentView
{
    public MapView()
    {
        InitializeComponent();
        ControlMapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
    }

    [AutoBindable(OnChanged = nameof(LocationChanged))]
    private Collection<Location> locations = new Collection<Models.Location.Location>();

    private void LocationChanged(Collection<Location> newLocations)
    {
        DrawLine(newLocations);

        AddStartAndEndPins(newLocations);
    }

    private void DrawLine(Collection<Location> newLocations)
    {
        ControlMapView.Drawables.Clear();

        var line = new Mapsui.UI.Maui.Polyline
        {
            StrokeWidth = 3,
            StrokeColor = Mapsui.UI.Maui.KnownColor.Red,
            IsClickable = true
        };

        foreach (var location in newLocations)
        {
            var position = new Position(location.Longitude, location.Latitude);
            line.Positions.Add(position);
        }

        ControlMapView.Drawables.Add(line);
    }

    private void AddStartAndEndPins(Collection<Location> newLocations)
    {
        if (newLocations.Count > 0)
        {
            var startPin = new Mapsui.UI.Maui.Pin(ControlMapView)
            {
                Label = "Start",
                Type = PinType.Svg,
                Position = new Position(newLocations[0].Longitude, newLocations[0].Latitude),
                Scale = 0.5F,
                Color = Colors.Green
            };

            var endPin = new Mapsui.UI.Maui.Pin(ControlMapView)
            {
                Label = "End",
                Type = PinType.Svg,
                Position = new Position(newLocations[^1].Longitude, newLocations[^1].Latitude),
                Scale = 0.5F,
                Color = Colors.Red
            };

            SetPinCallout(startPin, "Start", 10, 14, Colors.Black, Colors.Black);
            SetPinCallout(endPin, "Finish", 10, 14, Colors.Black, Colors.Black);

            ControlMapView.Pins.Add(startPin);
            ControlMapView.Pins.Add(endPin);

            var minLat = locations.Min(l => l.Latitude);
            var maxLat = locations.Max(l => l.Latitude);
            var minLon = locations.Min(l => l.Longitude);
            var maxLon = locations.Max(l => l.Longitude);

            MRect rect = new MRect(minLat, minLon, maxLat, maxLon);

            ControlMapView.Map.Navigator.ZoomToBox(rect);
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
