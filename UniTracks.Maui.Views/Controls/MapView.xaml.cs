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
    private MemoryLayer? pointsLayer;

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

        routeLayer = new MemoryLayer("Route")
        {
            Style = new Mapsui.Styles.VectorStyle
            {
                Line = new Mapsui.Styles.Pen(new Mapsui.Styles.Color(255, 0, 0), 3)
            }
        };

        if (projected.Length > 1)
        {
            var coordinates = projected.Select(point => new Coordinate(point.x, point.y)).ToArray();
            routeLayer.Features = new[] { new GeometryFeature(new LineString(coordinates)) };
        }

        var pointFeatures = new List<IFeature>();

        if (projected.Length > 0)
        {
            pointFeatures.Add(new PointFeature(projected[0].x, projected[0].y));
        }

        if (projected.Length > 1)
        {
            pointFeatures.Add(new PointFeature(projected[^1].x, projected[^1].y));
        }

        pointsLayer = new MemoryLayer("StartAndEnd")
        {
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolType = Mapsui.Styles.SymbolType.Ellipse,
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(0, 200, 0)),
                SymbolScale = 0.75
            },
            Features = pointFeatures
        };

        ControlMapView.Map.Layers.Add(routeLayer);
        ControlMapView.Map.Layers.Add(pointsLayer);

        CenterOnRoute(projected);
    }

    private void RemoveRouteLayers()
    {
        if (routeLayer is not null)
        {
            ControlMapView.Map.Layers.Remove(routeLayer);
            routeLayer = null;
        }

        if (pointsLayer is not null)
        {
            ControlMapView.Map.Layers.Remove(pointsLayer);
            pointsLayer = null;
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