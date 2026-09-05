using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using UniTracks.Games.CityBuilder;

namespace UniTracks.Maui.Views.Controls.Game;

/// <summary>
/// SkiaSharp-rendered isometric city map for the cozy city builder.
/// Draws grass tiles with per-tile variation, procedural vector buildings,
/// drifting clouds, fountain water shimmer, a drop-in bounce for freshly placed
/// buildings and a day-time tint. Supports pan, pinch-zoom and tap-to-select tiles.
/// </summary>
public class CityMapView : SKCanvasView
{
    private const double BaseTileWidth = 96;
    private const double BaseTileHeight = 48;

    private readonly CitySprites sprites = new();
    private readonly Random random = new();
    private readonly PedestrianSimulation pedestrians = new();
    private readonly List<Cloud> clouds = new();
    private readonly List<AmbientBird> birds = new();
    private readonly List<Sparkle> sparkles = new();
    private readonly HashSet<Guid> sparkledBuildings = new();
    private readonly IDispatcherTimer animationTimer;

    private double panX;
    private double panY;
    private double zoom = 1;

    private double panStartX;
    private double panStartY;
    private double touchStartX;
    private double touchStartY;
    private bool isPanning;
    private bool isPinching;
    private double pinchStartZoom = 1;
    private float pinchStartDistance;
    private float pinchStartScaleDistance;
    private readonly Dictionary<long, SKPoint> activeTouches = new();

    public CityMapView()
    {
        EnableTouchEvents = true;
        Touch += OnTouch;

        for (int i = 0; i < 3; i++)
        {
            clouds.Add(SpawnCloud(random.NextDouble() * 400));
        }

        for (int i = 0; i < 2; i++)
        {
            birds.Add(new AmbientBird { X = (float)(random.NextDouble() * 600), Offset = random.NextDouble() * 10 });
        }

        // ~30 fps ambient animation loop (clouds, water shimmer, drop-in bounce).
        animationTimer = Dispatcher.CreateTimer();
        animationTimer.Interval = TimeSpan.FromMilliseconds(33);
        animationTimer.Tick += (_, _) => InvalidateSurface();
        animationTimer.Start();
    }

    public static readonly BindableProperty CityProperty = BindableProperty.Create(
        nameof(City), typeof(CityState), typeof(CityMapView),
        propertyChanged: static (b, _, _) => ((CityMapView)b).InvalidateSurface());

    public CityState? City
    {
        get => (CityState?)GetValue(CityProperty);
        set => SetValue(CityProperty, value);
    }

    public static readonly BindableProperty GhostBuildingProperty = BindableProperty.Create(
        nameof(GhostBuilding), typeof(BuildingDefinition), typeof(CityMapView),
        propertyChanged: static (b, _, _) => ((CityMapView)b).InvalidateSurface());

    /// <summary>Building selected in the shop — shown as ghost preview on the hovered/tapped tile.</summary>
    public BuildingDefinition? GhostBuilding
    {
        get => (BuildingDefinition?)GetValue(GhostBuildingProperty);
        set => SetValue(GhostBuildingProperty, value);
    }

    public static readonly BindableProperty DemolishModeProperty = BindableProperty.Create(
        nameof(DemolishMode), typeof(bool), typeof(CityMapView),
        propertyChanged: static (b, _, _) => ((CityMapView)b).InvalidateSurface());

    public bool DemolishMode
    {
        get => (bool)GetValue(DemolishModeProperty);
        set => SetValue(DemolishModeProperty, value);
    }

    public static readonly BindableProperty TileTappedCommandProperty = BindableProperty.Create(
        nameof(TileTappedCommand), typeof(ICommand), typeof(CityMapView));

    public ICommand? TileTappedCommand
    {
        get => (ICommand?)GetValue(TileTappedCommandProperty);
        set => SetValue(TileTappedCommandProperty, value);
    }

    private (int X, int Y)? selectedTile;

    // Physical pixel size of the canvas, captured during paint — the single source of
    // truth for hit-testing so taps and rendering always agree (no Density math).
    private float canvasWidth;
    private float canvasHeight;

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvasWidth = info.Width;
        canvasHeight = info.Height;
        canvas.Clear();

        var city = City;
        if (city is null || city.Tiles.Count == 0)
        {
            DrawEmptyHint(canvas, info);
            return;
        }

        int grid = city.GridSize;
        double tileW = BaseTileWidth * zoom;
        double tileH = BaseTileHeight * zoom;

        // Center the diamond: grid corner (0,0) sits at the top middle of the map.
        double originX = info.Width / 2.0 + panX;
        double originY = info.Height / 2.0 - grid * tileH / 2.0 + panY;

        // Sky ambience behind the city.
        DrawSky(canvas, info);
        UpdateAndDrawClouds(canvas, info);
        UpdateAndDrawBirds(canvas, info);

        // Day-time tint factor: 0 at noon, 1 at midnight.
        double night = ComputeNightFactor();

        // Painter's algorithm: back row (y=0) first, so front tiles overlap correctly.
        for (int y = 0; y < grid; y++)
        {
            for (int x = 0; x < grid; x++)
            {
                var tile = city.GetTile(x, y)!;
                double sx = originX + (x - y) * tileW / 2.0;
                double sy = originY + (x + y) * tileH / 2.0;

                bool isSelected = selectedTile == (x, y);
                DrawTile(canvas, sx, sy, tileW, tileH, x, y, tile, isSelected, night);

                if (tile.BuildingId is not null)
                {
                    double scale = ComputeDropInScale(tile.PlacedAt);
                    sprites.Draw(canvas, tile.BuildingId, (float)sx, (float)sy, (float)tileW, (float)scale, (float)night);

                    // One-shot coin sparkle right after a placement lands.
                    if (scale < 1 && tile.PlacedBuildingId is Guid placedId && sparkledBuildings.Add(placedId))
                    {
                        SpawnSparkles((float)sx, (float)(sy - tileW * 0.4));
                    }

                    if (DemolishMode)
                    {
                        DrawDemolishMark(canvas, sx, sy, tileW);
                    }
                }
                else if (isSelected && GhostBuilding is not null)
                {
                    sprites.DrawGhost(canvas, GhostBuilding.Id, (float)sx, (float)sy, (float)tileW);
                }
            }
        }

        UpdateAndDrawPedestrians(canvas, city, originX, originY, tileW, tileH, night);
        UpdateAndDrawSparkles(canvas);
    }

    private static readonly SKColor[] PedestrianPalette =
    {
        new(214, 96, 90),   // warm red
        new(90, 140, 220),  // blue
        new(232, 178, 74),  // mustard
        new(150, 100, 200), // violet
        new(84, 180, 160),  // teal
        new(220, 130, 170), // pink
    };

    private void UpdateAndDrawPedestrians(SKCanvas canvas, CityState city,
        double originX, double originY, double tileW, double tileH, double night)
    {
        pedestrians.Update(city, 0.033);
        if (pedestrians.Pedestrians.Count == 0)
        {
            return;
        }

        double time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        // Painter's algorithm: sort back-to-front by iso depth so figures overlap correctly.
        foreach (var pedestrian in pedestrians.Pedestrians.OrderBy(p => p.RenderX + p.RenderY))
        {
            double sx = originX + (pedestrian.RenderX - pedestrian.RenderY) * tileW / 2.0;
            double sy = originY + (pedestrian.RenderX + pedestrian.RenderY) * tileH / 2.0;

            double size = tileW * (pedestrian.Kind == PedestrianKind.Child ? 0.16 : 0.22);
            double bob = pedestrian.IsWalking
                ? Math.Abs(Math.Sin(time * 8 + pedestrian.Phase)) * size * 0.15
                : 0;

            DrawPedestrian(canvas, (float)sx, (float)(sy - bob), (float)size, pedestrian, night);
        }
    }

    private static void DrawPedestrian(SKCanvas canvas, float x, float groundY, float size,
        Pedestrian pedestrian, double night)
    {
        var shirt = ApplyNight(PedestrianPalette[pedestrian.ColorIndex], night);
        var skin = ApplyNight(new SKColor(240, 200, 168), night);

        using var bodyPaint = new SKPaint { Color = shirt, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var headPaint = new SKPaint { Color = skin, Style = SKPaintStyle.Fill, IsAntialias = true };

        // Soft shadow on the tile.
        using var shadowPaint = new SKPaint { Color = new SKColor(0, 0, 0, 60), IsAntialias = true };
        canvas.DrawOval(x, groundY + size * 0.08f, size * 0.55f, size * 0.18f, shadowPaint);

        // Body capsule and head.
        float bodyTop = groundY - size * 0.95f;
        float bodyBottom = groundY - size * 0.1f;
        canvas.DrawRoundRect(new SKRoundRect(
            new SKRect(x - size * 0.28f, bodyTop, x + size * 0.28f, bodyBottom), size * 0.22f), bodyPaint);
        canvas.DrawCircle(x, bodyTop - size * 0.22f, size * 0.26f, headPaint);
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (e.InContact)
                {
                    activeTouches[e.Id] = e.Location;

                    if (activeTouches.Count == 2)
                    {
                        // Second finger down: switch to pinch mode.
                        var points = activeTouches.Values.ToArray();
                        pinchStartDistance = Distance(points[0], points[1]);
                        pinchStartScaleDistance = pinchStartDistance;
                        pinchStartZoom = zoom;
                        isPinching = true;
                        isPanning = false;
                        selectedTile = null;
                    }
                    else
                    {
                        touchStartX = e.Location.X;
                        touchStartY = e.Location.Y;
                        panStartX = panX;
                        panStartY = panY;
                        isPanning = false;
                    }

                    e.Handled = true;
                }
                break;

            case SKTouchAction.Moved:
                if (e.InContact && activeTouches.ContainsKey(e.Id))
                {
                    activeTouches[e.Id] = e.Location;

                    if (isPinching && activeTouches.Count >= 2)
                    {
                        var points = activeTouches.Values.ToArray();
                        float distance = Distance(points[0], points[1]);
                        if (pinchStartScaleDistance > 0)
                        {
                            zoom = Math.Clamp(pinchStartZoom * (distance / pinchStartScaleDistance), 0.6, 2.2);
                            InvalidateSurface();
                        }
                    }
                    else if (!isPinching)
                    {
                        double dx = e.Location.X - touchStartX;
                        double dy = e.Location.Y - touchStartY;
                        if (isPanning || Math.Abs(dx) + Math.Abs(dy) > 12)
                        {
                            isPanning = true;
                            panX = panStartX + dx;
                            panY = panStartY + dy;
                            selectedTile = null;
                            InvalidateSurface();
                        }
                    }

                    e.Handled = true;
                }
                break;

            case SKTouchAction.WheelChanged:
                zoom = Math.Clamp(zoom * (e.WheelDelta > 0 ? 1.1 : 0.9), 0.6, 2.2);
                InvalidateSurface();
                e.Handled = true;
                break;

            case SKTouchAction.Released:
                activeTouches.Remove(e.Id);

                if (isPinching)
                {
                    // Pinch ends when fewer than two fingers remain; the leftover finger
                    // restarts a potential pan from its current position.
                    isPinching = activeTouches.Count >= 2;
                    if (activeTouches.Count == 1)
                    {
                        var remaining = activeTouches.Values.First();
                        touchStartX = remaining.X;
                        touchStartY = remaining.Y;
                        panStartX = panX;
                        panStartY = panY;
                        isPanning = false;
                    }
                }
                else if (!isPanning && activeTouches.Count == 0 && City is not null)
                {
                    HandleTap(e.Location);
                }

                if (activeTouches.Count == 0)
                {
                    isPanning = false;
                }

                e.Handled = true;
                break;
        }
    }

    private void HandleTap(SKPoint point)
    {
        var city = City!;
        int grid = city.GridSize;
        double tileW = BaseTileWidth * zoom;
        double tileH = BaseTileHeight * zoom;
        double originX = canvasWidth / 2.0 + panX;
        double originY = canvasHeight / 2.0 - grid * tileH / 2.0 + panY;

        // Inverse isometric transform: screen → tile coordinates.
        double dx = point.X - originX;
        double dy = point.Y - originY;
        double fx = (dx / (tileW / 2.0) + dy / (tileH / 2.0)) / 2.0;
        double fy = (dy / (tileH / 2.0) - dx / (tileW / 2.0)) / 2.0;
        int tx = (int)Math.Floor(fx);
        int ty = (int)Math.Floor(fy);

        if (tx < 0 || ty < 0 || tx >= grid || ty >= grid)
        {
            selectedTile = null;
            InvalidateSurface();
            return;
        }

        selectedTile = (tx, ty);
        InvalidateSurface();

        var tile = city.GetTile(tx, ty);
        if (tile is not null && TileTappedCommand?.CanExecute(tile) == true)
        {
            TileTappedCommand.Execute(tile);
        }
    }

    private void DrawTile(SKCanvas canvas, double sx, double sy, double tileW, double tileH,
        int x, int y, CityTile tile, bool isSelected, double night)
    {
        // Deterministic per-tile grass variation.
        int hash = (x * 31 + y * 17) % 5;
        byte g = (byte)(168 + hash * 6);
        var top = new SKColor(96, g, 102);
        var edge = new SKColor(70, 128, 76);

        if (tile.IsEmpty && isSelected && GhostBuilding is not null)
        {
            top = new SKColor(120, 200, 130);
            edge = new SKColor(96, 170, 105);
        }

        using var topPaint = new SKPaint { Color = ApplyNight(top, night), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var edgePaint = new SKPaint { Color = ApplyNight(edge, night), Style = SKPaintStyle.Fill, IsAntialias = true };

        float hw = (float)(tileW / 2.0);
        float hh = (float)(tileH / 2.0);
        float cx = (float)sx;
        float cy = (float)sy;

        // Tile side (depth) for a subtle 3D feel.
        using (var sidePath = new SKPath())
        {
            sidePath.MoveTo(cx - hw, cy);
            sidePath.LineTo(cx, cy + hh);
            sidePath.LineTo(cx + hw, cy);
            sidePath.LineTo(cx + hw, cy + hh * 0.25f);
            sidePath.LineTo(cx, cy + hh * 1.25f);
            sidePath.LineTo(cx - hw, cy + hh * 0.25f);
            sidePath.Close();
            canvas.DrawPath(sidePath, edgePaint);
        }

        using var topPath = new SKPath();
        topPath.MoveTo(cx, cy - hh);
        topPath.LineTo(cx + hw, cy);
        topPath.LineTo(cx, cy + hh);
        topPath.LineTo(cx - hw, cy);
        topPath.Close();
        canvas.DrawPath(topPath, topPaint);

        if (isSelected)
        {
            using var stroke = new SKPaint
            {
                Color = new SKColor(77, 231, 144),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3,
                IsAntialias = true,
            };
            canvas.DrawPath(topPath, stroke);
        }
    }

    private void DrawDemolishMark(SKCanvas canvas, double sx, double sy, double tileW)
    {
        float r = (float)(tileW * 0.16);
        float cx = (float)sx;
        float cy = (float)(sy - tileW * 0.5);
        using var paint = new SKPaint { Color = new SKColor(230, 80, 80, 220), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var textPaint = new SKPaint { Color = SKColors.White, TextSize = r * 1.4f, IsAntialias = true, TextAlign = SKTextAlign.Center };
        canvas.DrawCircle(cx, cy, r, paint);
        canvas.DrawText("✕", cx, cy + r * 0.5f, textPaint);
    }

    private void DrawSky(SKCanvas canvas, SKImageInfo info)
    {
        double night = ComputeNightFactor();
        var topColor = Lerp(new SKColor(28, 34, 42), new SKColor(10, 12, 26), night);
        var bottomColor = Lerp(new SKColor(38, 48, 56), new SKColor(18, 22, 40), night);
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(0, info.Height),
                new[] { topColor, bottomColor }, null, SKShaderTileMode.Clamp),
        };
        canvas.DrawRect(new SKRect(0, 0, info.Width, info.Height), paint);
    }

    private void UpdateAndDrawClouds(SKCanvas canvas, SKImageInfo info)
    {
        float dt = 0.033f;
        foreach (var cloud in clouds)
        {
            cloud.X += cloud.Speed * dt;
            if (cloud.X - cloud.Width > info.Width)
            {
                cloud.Reset(-cloud.Width, info.Height, random);
            }

            using var paint = new SKPaint { Color = new SKColor(255, 255, 255, cloud.Alpha), IsAntialias = true };
            float cy = cloud.Y;
            canvas.DrawOval(cloud.X, cy, cloud.Width / 2, cloud.Width / 5, paint);
            canvas.DrawOval(cloud.X - cloud.Width / 4, cy + cloud.Width / 12, cloud.Width / 3, cloud.Width / 6, paint);
            canvas.DrawOval(cloud.X + cloud.Width / 4, cy + cloud.Width / 12, cloud.Width / 3, cloud.Width / 6, paint);
        }
    }

    private Cloud SpawnCloud(double startX) =>
        new()
        {
            X = (float)startX,
            Width = (float)(120 + random.NextDouble() * 140),
            Speed = (float)(6 + random.NextDouble() * 10),
            Alpha = (byte)(18 + random.Next(14)),
        };

    private void UpdateAndDrawBirds(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(220, 225, 230, 160),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        };

        double time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        foreach (var bird in birds)
        {
            bird.X += 55f * 0.033f;
            if (bird.X > info.Width + 30)
            {
                bird.X = -30;
                bird.Lane = 0.08 + random.NextDouble() * 0.2;
            }

            float y = (float)(info.Height * bird.Lane + Math.Sin(time * 1.3 + bird.Offset) * 8);
            float flap = (float)Math.Sin(time * 9 + bird.Offset) * 5;
            canvas.DrawLine(bird.X - 7, y + flap, bird.X, y, paint);
            canvas.DrawLine(bird.X, y, bird.X + 7, y + flap, paint);
        }
    }

    private void SpawnSparkles(float x, float y)
    {
        for (int i = 0; i < 10; i++)
        {
            double angle = random.NextDouble() * Math.PI * 2;
            double speed = 40 + random.NextDouble() * 80;
            sparkles.Add(new Sparkle
            {
                X = x,
                Y = y,
                Vx = (float)(Math.Cos(angle) * speed),
                Vy = (float)(Math.Sin(angle) * speed - 60),
                Life = 1,
            });
        }
    }

    private void UpdateAndDrawSparkles(SKCanvas canvas)
    {
        if (sparkles.Count == 0)
        {
            return;
        }

        const float dt = 0.033f;
        foreach (var sparkle in sparkles)
        {
            sparkle.X += sparkle.Vx * dt;
            sparkle.Y += sparkle.Vy * dt;
            sparkle.Vy += 220f * dt; // gravity
            sparkle.Life -= dt * 1.6f;
        }

        sparkles.RemoveAll(s => s.Life <= 0);

        foreach (var sparkle in sparkles)
        {
            byte alpha = (byte)(230 * Math.Clamp(sparkle.Life, 0, 1));
            using var paint = new SKPaint { Color = new SKColor(255, 215, 90, alpha), IsAntialias = true };
            canvas.DrawCircle(sparkle.X, sparkle.Y, 3f + 2f * sparkle.Life, paint);
        }
    }

    /// <summary>Drop-in bounce for buildings placed in the last 600 ms.</summary>
    private static double ComputeDropInScale(DateTimeOffset? placedAt)
    {
        if (placedAt is null)
        {
            return 1;
        }

        double elapsed = (DateTimeOffset.UtcNow - placedAt.Value).TotalMilliseconds;
        if (elapsed > 600)
        {
            return 1;
        }

        double t = elapsed / 600.0;
        // Overshoot then settle: simple ease-out-back.
        const double c1 = 1.70158;
        double c3 = c1 + 1;
        double v = 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
        return Math.Max(0.05, v);
    }

    /// <summary>0 at midday, 1 at midnight — drives sky and tint colors.</summary>
    private static double ComputeNightFactor()
    {
        double hour = DateTime.Now.TimeOfDay.TotalHours;
        double distance = Math.Abs(hour - 13.0); // brightest at 13:00
        return Math.Clamp((distance - 4.0) / 8.0, 0, 0.75);
    }

    private static SKColor ApplyNight(SKColor color, double night) =>
        Lerp(color, new SKColor((byte)(color.Red * 0.35), (byte)(color.Green * 0.4), (byte)(color.Blue * 0.55)), night);

    internal static SKColor Lerp(SKColor a, SKColor b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    private void DrawEmptyHint(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(180, 190, 200),
            TextSize = 18,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true,
        };
        canvas.DrawText("Stadt wird geladen …", info.Width / 2f, info.Height / 2f, paint);
    }

    private static float Distance(SKPoint a, SKPoint b) =>
        (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private sealed class Cloud
    {
        public float X { get; set; }
        public float Y { get; private set; }
        public float Width { get; set; }
        public float Speed { get; set; }
        public byte Alpha { get; set; }

        public void Reset(float startX, float maxHeight, Random random)
        {
            X = startX;
            Y = (float)(maxHeight * (0.05 + random.NextDouble() * 0.25));
        }
    }

    private sealed class AmbientBird
    {
        public float X { get; set; }
        public double Lane { get; set; } = 0.12;
        public double Offset { get; set; }
    }

    private sealed class Sparkle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }
        public float Life { get; set; }
    }
}
