using System.Diagnostics;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using UniTracks.Games.TowerDefense;

namespace UniTracks.Maui.Views.Controls.Game;

/// <summary>
/// SkiaSharp-rendered isometric map for the trail defense game, using the same
/// projection as <see cref="CityMapView"/>: grass/dirt diamonds with 3D side faces,
/// towers standing on their tiles, marching enemies with health bars and homing
/// projectiles. Drives the simulation by executing <see cref="TickCommand"/> from a
/// ~30 fps timer, forwards tile taps via <see cref="TileTappedCommand"/> and supports
/// pan and pinch-zoom.
/// </summary>
public class DefenseMapView : SKCanvasView
{
    /// <summary>Half the number of tiles along a diamond diagonal — sizes the iso layout.</summary>
    private const double HalfSpan = (DefensePath.GridWidth + DefensePath.GridHeight) / 2.0;

    private static readonly SKColor SkyTop = new(18, 26, 22);
    private static readonly SKColor SkyBottom = new(30, 44, 35);

    private readonly Stopwatch tickWatch = new();
    private readonly IDispatcherTimer gameTimer;

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
    private readonly Dictionary<long, SKPoint> activeTouches = new();

    /// <summary>Tile tapped most recently — anchors the ghost preview of the selected tower.</summary>
    private (int X, int Y)? selectedTile;

    // Physical pixel size of the canvas, captured during paint — the single source of
    // truth for hit-testing so taps and rendering always agree (no Density math).
    private float canvasWidth;
    private float canvasHeight;

    public DefenseMapView()
    {
        EnableTouchEvents = true;
        Touch += OnTouch;

        gameTimer = Dispatcher.CreateTimer();
        gameTimer.Interval = TimeSpan.FromMilliseconds(33);
        gameTimer.Tick += OnGameTick;
        gameTimer.Start();
        tickWatch.Start();
    }

    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(DefenseState), typeof(DefenseMapView),
        propertyChanged: static (b, _, _) => ((DefenseMapView)b).InvalidateSurface());

    public DefenseState? State
    {
        get => (DefenseState?)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public static readonly BindableProperty GhostTowerProperty = BindableProperty.Create(
        nameof(GhostTower), typeof(TowerDefinition), typeof(DefenseMapView),
        propertyChanged: static (b, _, _) => ((DefenseMapView)b).InvalidateSurface());

    /// <summary>Tower selected in the shop — shown as ghost preview with range indicator.</summary>
    public TowerDefinition? GhostTower
    {
        get => (TowerDefinition?)GetValue(GhostTowerProperty);
        set => SetValue(GhostTowerProperty, value);
    }

    public static readonly BindableProperty TileTappedCommandProperty = BindableProperty.Create(
        nameof(TileTappedCommand), typeof(ICommand), typeof(DefenseMapView));

    public ICommand? TileTappedCommand
    {
        get => (ICommand?)GetValue(TileTappedCommandProperty);
        set => SetValue(TileTappedCommandProperty, value);
    }

    public static readonly BindableProperty TickCommandProperty = BindableProperty.Create(
        nameof(TickCommand), typeof(ICommand), typeof(DefenseMapView));

    /// <summary>Executed every frame with the elapsed milliseconds — advances the simulation.</summary>
    public ICommand? TickCommand
    {
        get => (ICommand?)GetValue(TickCommandProperty);
        set => SetValue(TickCommandProperty, value);
    }

    /// <summary>Isometric projection state for the current canvas size, pan and zoom.</summary>
    private readonly record struct IsoLayout(double TileW, double TileH, double OriginX, double OriginY)
    {
        /// <summary>Maps a tile-space point (tile centers at x+0.5, y+0.5) to screen coordinates.</summary>
        public double ScreenX(double x, double y) => OriginX + (x - y) * TileW / 2.0;

        public double ScreenY(double x, double y) => OriginY + (x + y) * TileH / 2.0;
    }

    private IsoLayout CurrentLayout()
    {
        // Fit the full diamond width to the canvas; vertical space is centered.
        double tileW = canvasWidth * 0.96 / HalfSpan * zoom;
        double tileH = tileW / 2.0;
        double originX = canvasWidth / 2.0 + panX;
        double originY = (canvasHeight - HalfSpan * tileH) / 2.0 + panY;
        return new IsoLayout(tileW, tileH, originX, originY);
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        int elapsedMs = (int)Math.Min(tickWatch.ElapsedMilliseconds, 100);
        tickWatch.Restart();

        if (State is not null && TickCommand?.CanExecute(elapsedMs) == true)
        {
            TickCommand.Execute(elapsedMs);
        }

        InvalidateSurface();
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
                        if (pinchStartDistance > 0)
                        {
                            zoom = Math.Clamp(pinchStartZoom * (distance / pinchStartDistance), 0.6, 2.2);
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
                    isPinching = activeTouches.Count >= 2;
                }
                else if (!isPanning && activeTouches.Count == 0)
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
        var layout = CurrentLayout();

        // Inverse isometric transform: screen → tile coordinates.
        double dx = point.X - layout.OriginX;
        double dy = point.Y - layout.OriginY;
        double fx = (dx / (layout.TileW / 2.0) + dy / (layout.TileH / 2.0)) / 2.0;
        double fy = (dy / (layout.TileH / 2.0) - dx / (layout.TileW / 2.0)) / 2.0;
        int tx = (int)Math.Floor(fx);
        int ty = (int)Math.Floor(fy);

        if (tx < 0 || ty < 0 || tx >= DefensePath.GridWidth || ty >= DefensePath.GridHeight)
        {
            selectedTile = null;
            InvalidateSurface();
            return;
        }

        selectedTile = (tx, ty);
        InvalidateSurface();

        var tile = new DefenseTile(tx, ty);
        if (TileTappedCommand?.CanExecute(tile) == true)
        {
            TileTappedCommand.Execute(tile);
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvasWidth = e.Info.Width;
        canvasHeight = e.Info.Height;

        DrawBackground(canvas);

        if (State is null)
        {
            return;
        }

        var layout = CurrentLayout();
        DrawTiles(canvas, layout);
        DrawGhost(canvas, layout);
        DrawEnemies(canvas, layout);
        DrawProjectiles(canvas, layout);
    }

    private void DrawBackground(SKCanvas canvas)
    {
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(0, canvasHeight),
                new[] { SkyTop, SkyBottom }, null, SKShaderTileMode.Clamp),
        };
        canvas.DrawRect(new SKRect(0, 0, canvasWidth, canvasHeight), paint);
    }

    private void DrawTiles(SKCanvas canvas, IsoLayout layout)
    {
        // Painter's algorithm: back row (y=0) first, so front tiles overlap correctly.
        for (int y = 0; y < DefensePath.GridHeight; y++)
        {
            for (int x = 0; x < DefensePath.GridWidth; x++)
            {
                bool isGhostTarget = selectedTile == (x, y) && GhostTower is not null && (State?.IsBuildable(x, y) ?? false);
                DrawTile(canvas, layout, x, y, isGhostTarget);

                var tower = State?.TowerAt(x, y);
                if (tower is not null)
                {
                    DrawTower(canvas, layout, tower, alpha: 255);
                }
            }
        }

        // Trail entry and goal markers just outside the grid.
        DrawEmoji(canvas, "🌲", layout.ScreenX(2.5, -0.5), layout.ScreenY(2.5, -0.5), (float)(layout.TileW * 0.45));
        DrawEmoji(canvas, "⛺", layout.ScreenX(6.5, 15.5), layout.ScreenY(6.5, 15.5), (float)(layout.TileW * 0.4));
    }

    private void DrawTile(SKCanvas canvas, IsoLayout layout, int x, int y, bool isGhostTarget)
    {
        // Deterministic per-tile grass variation.
        int hash = (x * 31 + y * 17) % 5;
        byte g = (byte)(84 + hash * 5);
        var top = new SKColor(58, g, 64);
        var side = new SKColor(42, 62, 47);

        if (DefensePath.IsPath(x, y))
        {
            top = new SKColor(112, 89, 64);
            side = new SKColor(91, 71, 50);
        }
        else if (isGhostTarget)
        {
            top = new SKColor(96, 150, 108);
            side = new SKColor(72, 118, 84);
        }

        float cx = (float)layout.ScreenX(x + 0.5, y + 0.5);
        float cy = (float)layout.ScreenY(x + 0.5, y + 0.5);
        float hw = (float)(layout.TileW / 2.0);
        float hh = (float)(layout.TileH / 2.0);

        // Side faces for a subtle 3D feel.
        using (var sidePath = new SKPath())
        {
            sidePath.MoveTo(cx - hw, cy);
            sidePath.LineTo(cx, cy + hh);
            sidePath.LineTo(cx + hw, cy);
            sidePath.LineTo(cx + hw, cy + hh * 0.25f);
            sidePath.LineTo(cx, cy + hh * 1.25f);
            sidePath.LineTo(cx - hw, cy + hh * 0.25f);
            sidePath.Close();
            using var sidePaint = new SKPaint { Color = side, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(sidePath, sidePaint);
        }

        using var topPath = new SKPath();
        topPath.MoveTo(cx, cy - hh);
        topPath.LineTo(cx + hw, cy);
        topPath.LineTo(cx, cy + hh);
        topPath.LineTo(cx - hw, cy);
        topPath.Close();
        using var topPaint = new SKPaint { Color = top, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawPath(topPath, topPaint);

        if (isGhostTarget)
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

    private void DrawGhost(SKCanvas canvas, IsoLayout layout)
    {
        if (GhostTower is null || selectedTile is not { } tile || State is null || !State.IsBuildable(tile.X, tile.Y))
        {
            return;
        }

        double cx = layout.ScreenX(tile.X + 0.5, tile.Y + 0.5);
        double cy = layout.ScreenY(tile.X + 0.5, tile.Y + 0.5);

        // Range circle in tile space, sampled and projected (becomes an ellipse on screen).
        using var rangePath = new SKPath();
        const int segments = 28;
        for (int i = 0; i <= segments; i++)
        {
            double angle = i * 2.0 * Math.PI / segments;
            double px = tile.X + 0.5 + Math.Cos(angle) * GhostTower.RangeTiles;
            double py = tile.Y + 0.5 + Math.Sin(angle) * GhostTower.RangeTiles;
            float sx = (float)layout.ScreenX(px, py);
            float sy = (float)layout.ScreenY(px, py);
            if (i == 0)
            {
                rangePath.MoveTo(sx, sy);
            }
            else
            {
                rangePath.LineTo(sx, sy);
            }
        }

        rangePath.Close();
        using var rangePaint = new SKPaint { Color = new SKColor(255, 255, 255, 24), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawPath(rangePath, rangePaint);
        using var rangeStroke = new SKPaint { Color = new SKColor(255, 255, 255, 70), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
        canvas.DrawPath(rangePath, rangeStroke);

        DrawTowerBase(canvas, (float)cx, (float)cy, layout, GhostTower, alpha: 140);
        DrawEmoji(canvas, GhostTower.Icon, cx, cy - layout.TileH * 0.55, (float)(layout.TileW * 0.34), alpha: 180);
    }

    private void DrawTower(SKCanvas canvas, IsoLayout layout, PlacedTower tower, byte alpha)
    {
        var definition = TowerCatalog.Find(tower.TowerId);
        float cx = (float)layout.ScreenX(tower.X + 0.5, tower.Y + 0.5);
        float cy = (float)layout.ScreenY(tower.X + 0.5, tower.Y + 0.5);

        DrawTowerBase(canvas, cx, cy, layout, definition, alpha);
        DrawEmoji(canvas, definition?.Icon ?? "🗼", cx, cy - layout.TileH * 0.55, (float)(layout.TileW * 0.34), alpha);
    }

    private void DrawTowerBase(SKCanvas canvas, float cx, float cy, IsoLayout layout, TowerDefinition? definition, byte alpha)
    {
        float rx = (float)(layout.TileW * 0.17);
        float ry = (float)(layout.TileH * 0.3);

        using var shadowPaint = new SKPaint { Color = new SKColor(0, 0, 0, 60), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawOval(cx, cy + ry * 0.3f, rx * 1.15f, ry * 1.15f, shadowPaint);

        using var basePaint = new SKPaint
        {
            Color = SKColor.Parse(definition?.ColorHex ?? "#8BC34A").WithAlpha(alpha),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        canvas.DrawOval(cx, cy, rx, ry, basePaint);

        using var ringPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 90),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
        };
        canvas.DrawOval(cx, cy, rx, ry, ringPaint);
    }

    private void DrawEnemies(SKCanvas canvas, IsoLayout layout)
    {
        if (State is null)
        {
            return;
        }

        // Painter's algorithm: sort back-to-front by iso depth so figures overlap correctly.
        foreach (var enemy in State.Enemies.OrderBy(e => e.Position.X + e.Position.Y))
        {
            var (ex, ey) = enemy.Position;
            float px = (float)layout.ScreenX(ex, ey);
            float py = (float)layout.ScreenY(ex, ey);
            float size = (float)(layout.TileW * 0.3);

            using var shadowPaint = new SKPaint { Color = new SKColor(0, 0, 0, 60), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawOval(px, py + (float)(layout.TileH * 0.15), size * 0.4f, size * 0.14f, shadowPaint);

            DrawEmoji(canvas, enemy.Definition.Icon, px, py - layout.TileH * 0.35, size);

            // Health bar above the enemy (only when damaged).
            if (enemy.Hp < enemy.MaxHp)
            {
                float barWidth = (float)(layout.TileW * 0.28);
                float barHeight = Math.Max(3, (float)(layout.TileH * 0.1));
                float barLeft = px - barWidth / 2f;
                float barTop = py - (float)(layout.TileH * 0.35) - size * 0.75f;

                using var backPaint = new SKPaint { Color = new SKColor(0, 0, 0, 120), Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRect(new SKRect(barLeft, barTop, barLeft + barWidth, barTop + barHeight), backPaint);

                float fraction = Math.Max(0, (float)enemy.Hp / enemy.MaxHp);
                using var hpPaint = new SKPaint { Color = new SKColor(77, 231, 144), Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRect(new SKRect(barLeft, barTop, barLeft + barWidth * fraction, barTop + barHeight), hpPaint);
            }
        }
    }

    private void DrawProjectiles(SKCanvas canvas, IsoLayout layout)
    {
        if (State is null)
        {
            return;
        }

        foreach (var projectile in State.Projectiles)
        {
            using var paint = new SKPaint
            {
                Color = SKColor.Parse(projectile.ColorHex),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            canvas.DrawCircle(
                (float)layout.ScreenX(projectile.X, projectile.Y),
                (float)(layout.ScreenY(projectile.X, projectile.Y) - layout.TileH * 0.3),
                (float)(layout.TileW * 0.045),
                paint);
        }
    }

    private static void DrawEmoji(SKCanvas canvas, string emoji, double centerX, double centerY, float size, byte alpha = 255)
    {
        using var paint = new SKPaint
        {
            TextSize = size,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true,
            Color = SKColors.White.WithAlpha(alpha),
        };
        canvas.DrawText(emoji, (float)centerX, (float)centerY + size * 0.35f, paint);
    }

    private static float Distance(SKPoint a, SKPoint b) =>
        (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
