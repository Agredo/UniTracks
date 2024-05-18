using System.Collections.ObjectModel;
using Location = UniTracks.Models.Location.Location;
using Mapsui.UI.Maui;
using Maui.BindableProperty.Generator.Core;
using Mapsui;
using Mapsui.UI.Maui.Extensions;
using Mapsui.Fetcher;
using GeoCoordinatePortable;

namespace UniTracks.Maui.Views.Controls;

public partial class MapView : ContentView
{
    public MapView()
    {
        InitializeComponent();
        ControlMapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        ControlMapView.Loaded += ControlMapView_Loaded;
    }

    private void ControlMapView_Loaded(object? sender, EventArgs e)
    {
        var minLatitudeLocation = locations.OrderBy(x => x.Latitude).FirstOrDefault();
        var maxLatitudeLocation = locations.OrderBy(x => x.Latitude).LastOrDefault();
        var minLongitudeLocation = locations.OrderBy(x => x.Longitude).FirstOrDefault();
        var maxLongitudeLocation = locations.OrderBy(x => x.Longitude).LastOrDefault();

        if (minLatitudeLocation != null && maxLatitudeLocation != null && minLongitudeLocation != null && maxLongitudeLocation != null)
        {
            GeoCoordinate coordinate1 = new GeoCoordinate(maxLatitudeLocation.Latitude, maxLatitudeLocation.Longitude);
            GeoCoordinate coordinate2 = new GeoCoordinate(maxLongitudeLocation.Latitude, maxLongitudeLocation.Longitude);

            var centerLocation = new Position((minLongitudeLocation.Longitude + maxLongitudeLocation.Longitude) / 2, (minLatitudeLocation.Latitude + maxLatitudeLocation.Latitude) / 2);
            double maxDistance = coordinate1.GetDistanceTo(coordinate2);
            ControlMapView.Map.Navigator.CenterOnAndZoomTo(centerLocation.ToMapsui(), maxDistance + maxDistance * 0.25);

            this.ForceLayout();

            //ControlMapView.Map.Navigator.SetViewport(new Viewport(centerLocation.ToMapsui().X, centerLocation.ToMapsui().Y, maxDistance + maxDistance * 0.25, ControlMapView.Rotation, ControlMapView.Width, ControlMapView.Height));
        } 
    }

    [AutoBindable(OnChanged = nameof(LocationChanged))]
    private Collection<Location> locations = new Collection<Models.Location.Location>();

    private void LocationChanged(Collection<Location> newLocations)
    {
        locations = newLocations;
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
