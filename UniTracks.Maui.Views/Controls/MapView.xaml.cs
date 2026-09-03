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

        routeLayer = new MemoryLayer("Route");

        if (projected.Length > 1)
        {
            var speeds = ComputeSegmentSpeeds(locations);
            var minSpeed = speeds.Min();
            var maxSpeed = speeds.Max();

            var features = new List<IFeature>(projected.Length - 1);

            for (var index = 0; index < projected.Length - 1; index++)
            {
                var segment = new LineString(new[]
                {
                    new Coordinate(projected[index].x, projected[index].y),
                    new Coordinate(projected[index + 1].x, projected[index + 1].y),
                });

                var feature = new GeometryFeature(segment);
                feature.Styles.Add(new Mapsui.Styles.VectorStyle
                {
                    Line = new Mapsui.Styles.Pen(SpeedToColor(speeds[index], minSpeed, maxSpeed), 5)
                    {
                        PenStrokeCap = Mapsui.Styles.PenStrokeCap.Round
                    }
                });
                features.Add(feature);
            }

            routeLayer.Features = features;
        }
        else
        {
            var dotStyle = new Mapsui.Styles.VectorStyle
            {
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(255, 0, 0))
            };
            routeLayer.Style = dotStyle;
            routeLayer.Features = new[] { new PointFeature(projected[0].x, projected[0].y) };
        }

        ControlMapView.Map.Layers.Add(routeLayer);

        CenterOnRoute(projected);
    }

    private static double[] ComputeSegmentSpeeds(IReadOnlyList<Location> locations)
    {
        var speeds = new double[locations.Count - 1];

        for (var index = 0; index < speeds.Length; index++)
        {
            var from = locations[index];
            var to = locations[index + 1];
            var seconds = (to.Timestamp - from.Timestamp).TotalSeconds;

            if (from.Speed > 0 || to.Speed > 0)
            {
                speeds[index] = (from.Speed + to.Speed) / 2;
            }
            else if (seconds > 0)
            {
                speeds[index] = HaversineMeters(from.Latitude, from.Longitude, to.Latitude, to.Longitude) / seconds;
            }
            else
            {
                speeds[index] = 0;
            }
        }

        return speeds;
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // Lavender -> Mint -> Red, mapped onto the trip's speed range.
    private static Mapsui.Styles.Color SpeedToColor(double speed, double minSpeed, double maxSpeed)
    {
        var range = maxSpeed - minSpeed;
        var t = range > 0.001 ? (speed - minSpeed) / range : 0.5;

        (int r, int g, int b) from;
        (int r, int g, int b) to;

        if (t < 0.5)
        {
            from = (0xDF, 0xD8, 0xF7); // lavender
            to = (0x4D, 0xE7, 0x90);   // mint
            t *= 2;
        }
        else
        {
            from = (0x4D, 0xE7, 0x90); // mint
            to = (0xFF, 0x4D, 0x5E);   // record red
            t = (t - 0.5) * 2;
        }

        return new Mapsui.Styles.Color(
            (int)Math.Round(from.r + (to.r - from.r) * t),
            (int)Math.Round(from.g + (to.g - from.g) * t),
            (int)Math.Round(from.b + (to.b - from.b) * t));
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