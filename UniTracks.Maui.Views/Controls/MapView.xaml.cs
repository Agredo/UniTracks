using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using Coordinate = NetTopologySuite.Geometries.Coordinate;
using GeometryFeature = Mapsui.Nts.GeometryFeature;
using LineString = NetTopologySuite.Geometries.LineString;
using Location = UniTracks.Models.Location.Location;

namespace UniTracks.Maui.Views.Controls;

public partial class MapView : ContentView
{
    private MemoryLayer? routeLayer;

    public MapView()
    {
        InitializeComponent();
        ControlMapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        ControlMapView.Map.Navigator.RotationLock = true;
    }

    [BindableProperty(PropertyChangedMethodName = nameof(OnLocationsPropertyChanged))]
    public partial IReadOnlyList<Location>? Locations { get; set; }

    private static void OnLocationsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MapView mapView && newValue is IReadOnlyList<Location> locations)
        {
            mapView.DrawRoute(locations);
        }
    }

    private void DrawRoute(IReadOnlyList<Location> locations)
    {
        RemoveRouteLayers();

        if (locations.Count == 0)
        {
            return;
        }

        var projected = locations
            .Select(location => SphericalMercator.FromLonLat(location.Longitude, location.Latitude))
            .ToArray();

        var routeStyle = new Mapsui.Styles.VectorStyle
        {
            Line = new Mapsui.Styles.Pen(new Mapsui.Styles.Color(255, 0, 0), 4)
        };

        routeLayer = new MemoryLayer("Route") { Style = routeStyle };

        if (projected.Length > 1)
        {
            var coordinates = projected.Select(point => new Coordinate(point.x, point.y)).ToArray();
            routeLayer.Features = new[] { new GeometryFeature(new LineString(coordinates)) };
        }
        else
        {
            routeStyle.Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(255, 0, 0));
            routeLayer.Features = new[] { new PointFeature(projected[0].x, projected[0].y) };
        }

        ControlMapView.Map.Layers.Add(routeLayer);

        CenterOnRoute(projected);
    }

    private void RemoveRouteLayers()
    {
        if (routeLayer is not null)
        {
            ControlMapView.Map.Layers.Remove(routeLayer);
            routeLayer = null;
        }
    }

    private void CenterOnRoute((double x, double y)[] projected)
    {
        var minX = projected.Min(point => point.x);
        var maxX = projected.Max(point => point.x);
        var minY = projected.Min(point => point.y);
        var maxY = projected.Max(point => point.y);

        var paddingX = (maxX - minX) * 0.25;
        var paddingY = (maxY - minY) * 0.25;

        if (paddingX <= 0)
        {
            paddingX = 100;
        }

        if (paddingY <= 0)
        {
            paddingY = 100;
        }

        var box = new MRect(minX - paddingX, minY - paddingY, maxX + paddingX, maxY + paddingY);
        ControlMapView.Map.Navigator.ZoomToBox(box, MBoxFit.Fill);
    }
}