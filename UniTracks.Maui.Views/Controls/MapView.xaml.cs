using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using BruTile.Predefined;
using BruTile.Web;
using CommunityToolkit.Maui;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Manipulations;
using Mapsui.Projections;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using Coordinate = NetTopologySuite.Geometries.Coordinate;
using GeometryFeature = Mapsui.Nts.GeometryFeature;
using LineString = NetTopologySuite.Geometries.LineString;
using Location = UniTracks.Models.Location.Location;

namespace UniTracks.Maui.Views.Controls;

public partial class MapView : ContentView
{
    // Direction animation: the gap between two arrows is the route length divided into this many
    // steps, so one arrow travels one gap in roughly 1.3 seconds.
    private static readonly TimeSpan DirectionAnimationInterval = TimeSpan.FromMilliseconds(80);
    private const int DirectionAnimationStepsPerSpacing = 16;
    private const int DirectionArrowMinCount = 4;
    private const int DirectionArrowMaxCount = 12;
    private const double DirectionArrowSpacingTarget = 700;
    private const double DirectionArrowSymbolScale = 0.55;
    private const double DirectionFadeFraction = 0.12;

    private MemoryLayer? routeLayer;
    private MemoryLayer? directionLayer;
    private IDispatcherTimer? directionTimer;
    private (double x, double y)[] directionPath = [];
    private double[] directionCumulativeDistances = [];
    private double directionArrowSpacing;
    private double directionPhase;

    public MapView()
    {
        InitializeComponent();
        ControlMapView.Map.Layers.Add(CreateOpenStreetMapLayer());
        ControlMapView.Map.Navigator.RotationLock = true;
        ControlMapView.MapTapped += OnMapTapped;

        // The animation only runs while the view is on screen, so it costs nothing once the trip
        // page has been left.
        Loaded += OnMapViewLoaded;
        Unloaded += OnMapViewUnloaded;
    }

    private void OnMapViewLoaded(object? sender, EventArgs e)
    {
        if (directionPath.Length > 1)
        {
            StartDirectionAnimation();
        }
    }

    private void OnMapViewUnloaded(object? sender, EventArgs e) => StopDirectionAnimation();

    // The OpenStreetMap tile usage policy requires a User-Agent that identifies the app. Android's
    // native HTTP handler (HttpURLConnection, backed by OkHttp) replaces the header with a generic
    // client one, which the tile server rejects with an "Access blocked" tile. The layer therefore
    // uses its own HttpClient on the managed handler, which sends the header unchanged.
    private const string TileUserAgentFallbackVersion = "0.0.0";

    private static TileLayer CreateOpenStreetMapLayer()
    {
        var userAgent = $"UniTracks/{GetAppVersion()} (+https://github.com/Agredo/UniTracks)";

        var tileSource = new HttpTileSource(
            new GlobalSphericalMercator(),
            "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
            name: "OpenStreetMap",
            attribution: new BruTile.Attribution("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright"),
            configureHttpRequestMessage: request => request.Headers.TryAddWithoutValidation("User-Agent", userAgent));

        var httpClient = new HttpClient(new SocketsHttpHandler());
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

        return new TileLayer(tileSource, httpClient: httpClient) { Name = "OpenStreetMap" };
    }

    private static string GetAppVersion()
    {
        try
        {
            return AppInfo.Current.VersionString;
        }
        catch
        {
            return TileUserAgentFallbackVersion;
        }
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

    public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
        nameof(TapCommand), typeof(ICommand), typeof(MapView));

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    // The Mapsui MapControl hosts a SkiaSharp view that marks every touch as handled, so a
    // TapGestureRecognizer on this ContentView never fires on Android. Mapsui's own tap detection
    // is used instead; it ignores movements beyond MaxTapGestureMovement, which keeps panning and
    // zooming unaffected.
    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        if (e.GestureType != GestureType.SingleTap)
        {
            return;
        }

        if (TapCommand?.CanExecute(null) == true)
        {
            TapCommand.Execute(null);
        }
    }

    private void DrawRoute(IReadOnlyList<Location> locations)
    {
        RemoveRouteLayers();

        if (locations.Count == 0)
        {
            return;
        }

        // Post-processing: draw the smoothed track (GPS jitter filtered/averaged). Raw points
        // stay untouched in the database. The smoother may drop every point (e.g. all fixes
        // with bad accuracy) — then there is simply nothing to draw.
        var smoothed = UniTracks.Services.Location.TrackSmoother.Smooth(locations);

        if (smoothed.Count == 0)
        {
            return;
        }

        var projected = smoothed
            .Select(location => SphericalMercator.FromLonLat(location.Longitude, location.Latitude))
            .ToArray();

        routeLayer = new MemoryLayer("Route");

        if (projected.Length > 1)
        {
            var speeds = ComputeSegmentSpeeds(smoothed);
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

        // Direction of travel: arrows that march from start to finish along the smoothed track,
        // plus a checkered finish flag on the last point.
        CreateDirectionLayer(projected);

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
        StopDirectionAnimation();

        if (directionLayer is not null)
        {
            ControlMapView.Map.Layers.Remove(directionLayer);
            directionLayer = null;
        }

        if (routeLayer is not null)
        {
            ControlMapView.Map.Layers.Remove(routeLayer);
            routeLayer = null;
        }

        directionPath = [];
        directionCumulativeDistances = [];
        directionArrowSpacing = 0;
        directionPhase = 0;
    }

    // ---- Direction of travel ---------------------------------------------------------------

    // The checkered finish flag is embedded as an inline SVG, so neither an asset file nor a
    // network request is needed. Mapsui resolves "svg-content://" through its own image cache.
    private static readonly string FinishFlagSource = "svg-content://" + BuildFinishFlagSvg();
    private static readonly Mapsui.Styles.Image FinishFlagImage = new() { Source = FinishFlagSource };

    private static string BuildFinishFlagSvg()
    {
        const int columns = 6;
        const int rows = 5;
        const double cell = 3;
        const double flagX = 20.8;
        const double flagY = 3;
        const double flagWidth = columns * cell;
        const double flagHeight = rows * cell;

        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"40\" height=\"40\" viewBox=\"0 0 40 40\">");
        svg.Append("<rect x=\"19\" y=\"3\" width=\"2\" height=\"36\" rx=\"1\" fill=\"#1B1B1B\"/>");
        svg.Append($"<rect x=\"{flagX}\" y=\"{flagY}\" width=\"{flagWidth}\" height=\"{flagHeight}\" fill=\"#FFFFFF\"/>");

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                if ((row + column) % 2 == 0)
                {
                    continue;
                }

                svg.Append($"<rect x=\"{flagX + (column * cell)}\" y=\"{flagY + (row * cell)}\" width=\"{cell}\" height=\"{cell}\" fill=\"#111111\"/>");
            }
        }

        svg.Append($"<rect x=\"{flagX}\" y=\"{flagY}\" width=\"{flagWidth}\" height=\"{flagHeight}\" fill=\"none\" stroke=\"#111111\" stroke-width=\"1\"/>");
        svg.Append("</svg>");
        return svg.ToString();
    }

    private void CreateDirectionLayer((double x, double y)[] projected)
    {
        StopDirectionAnimation();

        directionPath = projected;
        directionPhase = 0;
        directionCumulativeDistances = new double[projected.Length];

        for (var index = 1; index < projected.Length; index++)
        {
            var dx = projected[index].x - projected[index - 1].x;
            var dy = projected[index].y - projected[index - 1].y;
            directionCumulativeDistances[index] =
                directionCumulativeDistances[index - 1] + Math.Sqrt((dx * dx) + (dy * dy));
        }

        var totalLength = directionCumulativeDistances[^1];

        var features = new List<IFeature> { CreateFinishFlagFeature(projected[^1]) };

        if (projected.Length > 1 && totalLength > 0)
        {
            var arrowCount = Math.Clamp(
                (int)Math.Round(totalLength / DirectionArrowSpacingTarget),
                DirectionArrowMinCount,
                DirectionArrowMaxCount);

            directionArrowSpacing = totalLength / arrowCount;
            features.AddRange(CreateDirectionArrowFeatures(totalLength));
        }

        directionLayer = new MemoryLayer("RouteDirection") { Style = null, Features = features };
        ControlMapView.Map.Layers.Add(directionLayer);

        StartDirectionAnimation();
    }

    private static IFeature CreateFinishFlagFeature((double x, double y) point)
    {
        var feature = new PointFeature(point.x, point.y);
        feature.Styles.Add(new Mapsui.Styles.ImageStyle
        {
            Image = FinishFlagImage,
            RotateWithMap = false,
            SymbolScale = 1.1,
            // A relative offset of (0, 0.5) moves the image up by half its height, so the foot of
            // the flag pole ends up exactly on the last track point.
            RelativeOffset = new Mapsui.Styles.RelativeOffset(0, 0.5),
        });
        return feature;
    }

    private List<IFeature> CreateDirectionArrowFeatures(double totalLength)
    {
        var features = new List<IFeature>();
        var fadeLength = Math.Min(directionArrowSpacing, totalLength * DirectionFadeFraction);

        for (var distance = directionPhase; distance <= totalLength; distance += directionArrowSpacing)
        {
            if (!TryLocateOnPath(distance, out var x, out var y, out var bearing))
            {
                continue;
            }

            // Arrows fade in at the start and out at the finish so that the marching motion has no
            // hard pop when an arrow enters or leaves the route.
            var opacity = fadeLength > 0
                ? (float)Math.Min(1, Math.Min(distance, totalLength - distance) / fadeLength)
                : 1f;

            var feature = new PointFeature(x, y);
            feature.Styles.Add(new Mapsui.Styles.SymbolStyle
            {
                SymbolType = Mapsui.Styles.SymbolType.Triangle,
                SymbolRotation = bearing,
                RotateWithMap = false,
                SymbolScale = DirectionArrowSymbolScale,
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(0xF2, 0xF7, 0xF3)),
                Outline = new Mapsui.Styles.Pen(new Mapsui.Styles.Color(0x0C, 0x12, 0x0E), 1.4f),
                Opacity = opacity,
            });
            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Interpolates the point that lies <paramref name="distance"/> along the projected track and
    /// the rotation (clockwise degrees) that points a symbol in the direction of travel there.
    /// </summary>
    private bool TryLocateOnPath(double distance, out double x, out double y, out double bearingDegrees)
    {
        x = 0;
        y = 0;
        bearingDegrees = 0;

        if (directionPath.Length < 2)
        {
            return false;
        }

        var index = 1;
        while (index < directionCumulativeDistances.Length - 1 && directionCumulativeDistances[index] < distance)
        {
            index++;
        }

        var from = directionPath[index - 1];
        var to = directionPath[index];
        var segmentLength = directionCumulativeDistances[index] - directionCumulativeDistances[index - 1];
        var t = segmentLength > 0
            ? Math.Clamp((distance - directionCumulativeDistances[index - 1]) / segmentLength, 0, 1)
            : 0;

        x = from.x + ((to.x - from.x) * t);
        y = from.y + ((to.y - from.y) * t);

        // Screen Y grows downwards while Mercator Y grows northwards, and a symbol that points up
        // by default is rotated clockwise, so the angle is atan2(dx, dy) rather than atan2(dy, dx).
        bearingDegrees = Math.Atan2(to.x - from.x, to.y - from.y) * 180 / Math.PI;
        return true;
    }

    private void StartDirectionAnimation()
    {
        StopDirectionAnimation();

        if (directionLayer is null || directionPath.Length < 2 || directionArrowSpacing <= 0)
        {
            return;
        }

        if (Application.Current?.Dispatcher is not { } dispatcher)
        {
            return;
        }

        var timer = dispatcher.CreateTimer();
        timer.Interval = DirectionAnimationInterval;
        timer.Tick += OnDirectionAnimationTick;
        directionTimer = timer;
        timer.Start();
    }

    private void StopDirectionAnimation()
    {
        if (directionTimer is null)
        {
            return;
        }

        directionTimer.Tick -= OnDirectionAnimationTick;
        directionTimer.Stop();
        directionTimer = null;
    }

    private void OnDirectionAnimationTick(object? sender, EventArgs e)
    {
        if (directionLayer is null || directionPath.Length < 2 || directionArrowSpacing <= 0)
        {
            StopDirectionAnimation();
            return;
        }

        directionPhase += directionArrowSpacing / DirectionAnimationStepsPerSpacing;
        if (directionPhase >= directionArrowSpacing)
        {
            directionPhase -= directionArrowSpacing;
        }

        var totalLength = directionCumulativeDistances[^1];
        var features = new List<IFeature> { CreateFinishFlagFeature(directionPath[^1]) };
        features.AddRange(CreateDirectionArrowFeatures(totalLength));
        directionLayer.Features = features;

        ControlMapView.RefreshGraphics();
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