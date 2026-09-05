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

    // Reusable Skia objects for the board, so a full redraw doesn't allocate hundreds of
    // native paint/path objects every frame (which grows as later waves add more enemies).
    private readonly SKPath tileSidePath = new();
    private readonly SKPath tileTopPath = new();
    private readonly SKPaint tileSidePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint tileTopPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint tileGhostStrokePaint = new()
    {
        Color = new SKColor(77, 231, 144),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3,
        IsAntialias = true,
    };

    private readonly Stopwatch tickWatch = new();
    private readonly IDispatcherTimer gameTimer;

    /// <summary>Accumulated simulation time — drives animated attack effects without touching the game model.</summary>
    private double effectTimeMs;

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
        effectTimeMs += elapsedMs;

        try
        {
            if (State is not null && TickCommand?.CanExecute(elapsedMs) == true)
            {
                TickCommand.Execute(elapsedMs);
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write($"Game tick failed: {ex}");
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

        // During layout the canvas can still be 0-sized, which would make the inverse
        // transform divide by zero and overflow when converted back to a tile index.
        if (layout.TileW <= 0 || layout.TileH <= 0)
        {
            return;
        }

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

        try
        {
            DrawBackground(canvas);

            if (State is null)
            {
                return;
            }

            var layout = CurrentLayout();
            DrawTiles(canvas, layout);
            DrawGhost(canvas, layout);
            DrawTowerEffects(canvas, layout);
            DrawEnemies(canvas, layout);
            DrawProjectiles(canvas, layout);
        }
        catch (Exception ex)
        {
            // A single bad frame must never take the whole game down.
            CrashLog.Write($"Paint failed: {ex}");
        }
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

        // Trail entry and goal markers just outside the grid (vector, since emoji glyphs
        // render inconsistently on some platforms).
        DrawPineTree(canvas, (float)layout.ScreenX(2.5, -0.5), (float)layout.ScreenY(2.5, -0.5), (float)(layout.TileW * 0.42));
        DrawTent(canvas, (float)layout.ScreenX(6.5, 15.5), (float)layout.ScreenY(6.5, 15.5), (float)(layout.TileW * 0.42));
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

        // Side faces for a subtle 3D feel. Path/paint instances are reused across tiles
        // so the board stays cheap to redraw every frame (135 tiles * many objects/frame).
        tileSidePaint.Color = side;
        tileSidePath.Rewind();
        tileSidePath.MoveTo(cx - hw, cy);
        tileSidePath.LineTo(cx, cy + hh);
        tileSidePath.LineTo(cx + hw, cy);
        tileSidePath.LineTo(cx + hw, cy + hh * 0.25f);
        tileSidePath.LineTo(cx, cy + hh * 1.25f);
        tileSidePath.LineTo(cx - hw, cy + hh * 0.25f);
        tileSidePath.Close();
        canvas.DrawPath(tileSidePath, tileSidePaint);

        tileTopPaint.Color = top;
        tileTopPath.Rewind();
        tileTopPath.MoveTo(cx, cy - hh);
        tileTopPath.LineTo(cx + hw, cy);
        tileTopPath.LineTo(cx, cy + hh);
        tileTopPath.LineTo(cx - hw, cy);
        tileTopPath.Close();
        canvas.DrawPath(tileTopPath, tileTopPaint);

        if (isGhostTarget)
        {
            canvas.DrawPath(tileTopPath, tileGhostStrokePaint);
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
        DrawTowerSprite(canvas, GhostTower, cx, cy - layout.TileH * 0.55, (float)(layout.TileW * 0.34), effectTimeMs, alpha: 180);
    }

    private void DrawTower(SKCanvas canvas, IsoLayout layout, PlacedTower tower, byte alpha)
    {
        var definition = TowerCatalog.Find(tower.TowerId);
        float cx = (float)layout.ScreenX(tower.X + 0.5, tower.Y + 0.5);
        float cy = (float)layout.ScreenY(tower.X + 0.5, tower.Y + 0.5);

        DrawTowerBase(canvas, cx, cy, layout, definition, alpha);
        if (definition is not null)
        {
            DrawTowerSprite(canvas, definition, cx, cy - layout.TileH * 0.55, (float)(layout.TileW * 0.34), effectTimeMs, alpha);
        }
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
            // Boss insects render slightly larger so they read as heavy hitters.
            float size = (float)(layout.TileW * (enemy.Definition.Id is "wasp" or "hornet" ? 0.34 : 0.3));

            using var shadowPaint = new SKPaint { Color = new SKColor(0, 0, 0, 60), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawOval(px, py + (float)(layout.TileH * 0.15), size * 0.4f, size * 0.14f, shadowPaint);

            DrawInsect(canvas, enemy.Definition.Id, px, (float)(py - layout.TileH * 0.35), size, effectTimeMs + enemy.Id * 137);

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

    /// <summary>Draws persistent ambient effects (the candle's scent cloud) over the tiles.</summary>
    private void DrawTowerEffects(SKCanvas canvas, IsoLayout layout)
    {
        if (State is null)
        {
            return;
        }

        foreach (var tower in State.Towers)
        {
            var def = TowerCatalog.Find(tower.TowerId);
            if (def?.AttackStyle != AttackStyle.Cloud)
            {
                continue;
            }

            float cx = (float)layout.ScreenX(tower.X + 0.5, tower.Y + 0.5);
            float cy = (float)layout.ScreenY(tower.X + 0.5, tower.Y + 0.5) - (float)(layout.TileH * 0.9);
            DrawScentCloud(canvas, cx, cy, (float)(layout.TileW * 0.32), effectTimeMs + tower.X * 29 + tower.Y * 53);
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
            DrawAttackEffect(canvas, layout, projectile);
        }
    }

    private static void DrawAttackEffect(SKCanvas canvas, IsoLayout layout, ActiveProjectile projectile)
    {
        var color = SKColor.Parse(projectile.ColorHex);
        float ox = (float)layout.ScreenX(projectile.OriginX, projectile.OriginY);
        float oy = (float)layout.ScreenY(projectile.OriginX, projectile.OriginY) - (float)(layout.TileH * 0.45);
        float tx = (float)layout.ScreenX(projectile.X, projectile.Y);
        float ty = (float)layout.ScreenY(projectile.X, projectile.Y) - (float)(layout.TileH * 0.3);

        switch (projectile.AttackStyle)
        {
            case AttackStyle.Spray:
                DrawSprayEffect(canvas, ox, oy, tx, ty, (float)(layout.TileW * 0.03), color);
                break;
            case AttackStyle.Cloud:
                DrawCloudEffect(canvas, tx, ty, (float)(layout.TileW * 0.13), color);
                break;
            case AttackStyle.Zap:
                DrawZapEffect(canvas, ox, oy, tx, ty, (float)(layout.TileW * 0.028), color);
                break;
            case AttackStyle.Tongue:
                DrawTongueEffect(canvas, ox, oy, tx, ty);
                break;
        }
    }

    /// <summary>A fan of drifting aerosol droplets — looks like a real spray burst.</summary>
    private static void DrawSprayEffect(SKCanvas canvas, float ox, float oy, float tx, float ty, float dropRadius, SKColor color)
    {
        float dx = tx - ox;
        float dy = ty - oy;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.01f)
        {
            return;
        }

        float ux = dx / length;
        float uy = dy / length;
        float px = -uy;
        float py = ux;

        using (var cone = new SKPath())
        {
            cone.MoveTo(ox, oy);
            cone.LineTo(tx - ux * length * 0.1f + px * length * 0.16f, ty - uy * length * 0.1f + py * length * 0.16f);
            cone.LineTo(tx - ux * length * 0.1f - px * length * 0.16f, ty - uy * length * 0.1f - py * length * 0.16f);
            cone.Close();
            using var conePaint = new SKPaint { Color = color.WithAlpha(46), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(cone, conePaint);
        }

        using var dropPaint = new SKPaint { Color = color.WithAlpha(205), Style = SKPaintStyle.Fill, IsAntialias = true };
        for (int i = 0; i < 6; i++)
        {
            double phase = (EffectClock() / 45.0 + i * 1.13) % (2 * Math.PI);
            float aim = (float)Math.Sin(phase) * length * 0.16f;
            float progress = (float)((EffectClock() / 60.0 + i * 0.17) % 1.0);
            float x = ox + ux * length * progress + px * aim;
            float y = oy + uy * length * progress + py * aim;
            float radius = Math.Max(0.8f, dropRadius * (0.7f + 0.6f * (float)Math.Cos(phase)));
            canvas.DrawCircle(x, y, radius, dropPaint);
        }
    }

    /// <summary>A soft incense puff that travels with the projectile — the candle's scent cloud.</summary>
    private static void DrawCloudEffect(SKCanvas canvas, float tx, float ty, float baseRadius, SKColor color)
    {
        float pulse = 1f + 0.25f * (float)Math.Sin(EffectClock() / 120.0);
        float radius = baseRadius * pulse;

        using var puffPaint = new SKPaint { Color = color.WithAlpha(90), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(tx, ty, radius, puffPaint);
        using var corePaint = new SKPaint { Color = new SKColor(255, 250, 240, 150), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(tx - radius * 0.15f, ty - radius * 0.15f, radius * 0.5f, corePaint);
    }

    /// <summary>A flickering zig-zag lightning bolt from the tower to the target.</summary>
    private static void DrawZapEffect(SKCanvas canvas, float ox, float oy, float tx, float ty, float width, SKColor color)
    {
        float dx = tx - ox;
        float dy = ty - oy;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.01f)
        {
            return;
        }

        float ux = dx / length;
        float uy = dy / length;
        float px = -uy;
        float py = ux;

        using (var glowPath = new SKPath())
        {
            BuildZigZag(glowPath, ox, oy, ux, uy, px, py, length);
            using var glow = new SKPaint { Color = color.WithAlpha(90), Style = SKPaintStyle.Stroke, StrokeWidth = width * 4f, IsAntialias = true };
            canvas.DrawPath(glowPath, glow);
        }

        using (var boltPath = new SKPath())
        {
            BuildZigZag(boltPath, ox, oy, ux, uy, px, py, length);
            using var bolt = new SKPaint { Color = new SKColor(255, 255, 255, 230), Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1.5f, width), IsAntialias = true };
            canvas.DrawPath(boltPath, bolt);
        }
    }

    private static void BuildZigZag(SKPath path, float ox, float oy, float ux, float uy, float px, float py, float length)
    {
        const int segments = 6;
        path.MoveTo(ox, oy);
        for (int i = 1; i <= segments; i++)
        {
            float s = length * i / segments;
            float jitter = (float)Math.Sin(EffectClock() / 30.0 + i * 2.7) * length * 0.12f;
            path.LineTo(ox + ux * s + px * jitter, oy + uy * s + py * jitter);
        }
    }

    /// <summary>A tapered tongue from the tower to the projectile tip (frog / gecko).</summary>
    private static void DrawTongueEffect(SKCanvas canvas, float ox, float oy, float tx, float ty)
    {
        float dx = tx - ox;
        float dy = ty - oy;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.01f)
        {
            return;
        }

        float ux = dx / length;
        float uy = dy / length;
        float px = -uy;
        float py = ux;
        float throat = length * 0.09f;
        float tipWidth = length * 0.02f;

        using (var tonguePath = new SKPath())
        {
            tonguePath.MoveTo(ox + px * throat, oy + py * throat);
            tonguePath.LineTo(tx + px * tipWidth, ty + py * tipWidth);
            tonguePath.LineTo(tx - px * tipWidth, ty - py * tipWidth);
            tonguePath.LineTo(ox - px * throat, oy - py * throat);
            tonguePath.Close();

            using var fill = new SKPaint { Color = new SKColor(230, 120, 130, 220), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(tonguePath, fill);
            using var outline = new SKPaint { Color = new SKColor(150, 40, 60, 180), Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
            canvas.DrawPath(tonguePath, outline);
        }
    }

    /// <summary>A translucent, rising halo of the candle's scent cloud.</summary>
    private static void DrawScentCloud(SKCanvas canvas, float cx, float cy, float baseRadius, double phase)
    {
        float drift = (float)(Math.Sin(phase / 140.0) * baseRadius * 0.25);
        using var paint = new SKPaint { Color = new SKColor(255, 214, 165, 70), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawOval(cx + drift, cy, baseRadius * 0.9f, baseRadius * 0.55f, paint);
        canvas.DrawOval(cx - drift * 0.6f - baseRadius * 0.3f, cy - baseRadius * 0.25f, baseRadius * 0.4f, baseRadius * 0.3f, paint);
        canvas.DrawOval(cx + drift * 0.5f + baseRadius * 0.3f, cy - baseRadius * 0.2f, baseRadius * 0.42f, baseRadius * 0.3f, paint);
    }

    /// <summary>Vector enemy sprites — no emoji, so insects render consistently everywhere.</summary>
    private static void DrawInsect(SKCanvas canvas, string id, float px, float py, float size, double phase)
    {
        float flap = (float)(Math.Sin(phase / 60.0) * size * 0.09);
        float bob = (float)(Math.Sin(phase / 95.0) * size * 0.05);
        float cx = px;
        float cy = py + bob;

        var body = id switch
        {
            "wasp" => new SKColor(240, 200, 40),
            "hornet" => new SKColor(210, 120, 40),
            "gnat" => new SKColor(150, 120, 90),
            _ => new SKColor(96, 100, 110),
        };
        var dark = id is "wasp" or "hornet" ? new SKColor(40, 30, 20) : new SKColor(55, 55, 62);

        // Very slim, needle-like mosquito proportions.
        float bodyWidth = size * 1.85f;
        float bodyHeight = size * 0.34f;

        // Narrow, swept-back translucent wings drawn as thin curved blades that
        // flare out sideways/backward from the thorax — like a real mosquito.
        using (var wingPaint = new SKPaint { Color = new SKColor(222, 230, 245).WithAlpha(140), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            float wingRootX = cx + bodyWidth * 0.05f;
            float wingRootY = cy - bodyHeight * 0.35f;
            float wingLen = bodyWidth * 0.85f;
            float wingWidth = bodyHeight * 0.5f;

            // Upper (far) wing — angled slightly up-back.
            using (var upperWing = new SKPath())
            {
                upperWing.MoveTo(wingRootX, wingRootY);
                upperWing.CubicTo(
                    wingRootX - wingLen * 0.4f, wingRootY - wingWidth * 1.6f + flap,
                    wingRootX - wingLen * 0.9f, wingRootY - wingWidth * 1.1f + flap,
                    wingRootX - wingLen, wingRootY - wingWidth * 0.2f + flap);
                upperWing.CubicTo(
                    wingRootX - wingLen * 0.7f, wingRootY + wingWidth * 0.3f + flap,
                    wingRootX - wingLen * 0.25f, wingRootY + wingWidth * 0.4f,
                    wingRootX, wingRootY);
                upperWing.Close();
                canvas.DrawPath(upperWing, wingPaint);
            }

            // Lower (near) wing — angled down-back.
            using (var lowerWing = new SKPath())
            {
                lowerWing.MoveTo(wingRootX, wingRootY + bodyHeight * 0.3f);
                lowerWing.CubicTo(
                    wingRootX - wingLen * 0.35f, wingRootY + wingWidth * 1.5f - flap,
                    wingRootX - wingLen * 0.85f, wingRootY + wingWidth * 1.0f - flap,
                    wingRootX - wingLen * 0.95f, wingRootY + wingWidth * 0.2f - flap);
                lowerWing.CubicTo(
                    wingRootX - wingLen * 0.6f, wingRootY - wingWidth * 0.2f - flap,
                    wingRootX - wingLen * 0.2f, wingRootY - wingWidth * 0.3f,
                    wingRootX, wingRootY + bodyHeight * 0.3f);
                lowerWing.Close();
                canvas.DrawPath(lowerWing, wingPaint);
            }
        }

        // Long, thin dangly legs — spindlier than before, angled outward.
        using (var legPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1, size * 0.04f), IsAntialias = true })
        {
            for (int i = -1; i <= 1; i++)
            {
                float lx = cx + i * bodyWidth * 0.28f;
                canvas.DrawLine(lx, cy + bodyHeight * 0.15f, lx - bodyWidth * 0.22f, cy + bodyHeight * 1.5f, legPaint);
                canvas.DrawLine(lx, cy + bodyHeight * 0.15f, lx + bodyWidth * 0.22f, cy + bodyHeight * 1.5f, legPaint);
            }
        }

        // Slender body.
        using (var bodyPaint = new SKPaint { Color = body, Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(cx - bodyWidth * 0.5f, cy - bodyHeight * 0.5f, cx + bodyWidth * 0.5f, cy + bodyHeight * 0.5f), bodyHeight * 0.5f), bodyPaint);
        }

        // Long tapered abdomen (pointed rear).
        using (var abdomenPaint = new SKPaint { Color = body, Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            using var abdomen = new SKPath();
            abdomen.MoveTo(cx - bodyWidth * 0.5f, cy);
            abdomen.LineTo(cx - bodyWidth * 0.95f, cy - bodyHeight * 0.12f);
            abdomen.LineTo(cx - bodyWidth * 0.95f, cy + bodyHeight * 0.12f);
            abdomen.Close();
            canvas.DrawPath(abdomen, abdomenPaint);
        }

        // Menacing red eyes on wasps / hornets / boss — otherwise dark.
        float headX = cx + bodyWidth * 0.5f;
        float headRadius = size * 0.2f;
        using (var headPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            canvas.DrawCircle(headX, cy, headRadius, headPaint);
        }

        using (var eyePaint = new SKPaint { Color = new SKColor(220, 40, 40), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            canvas.DrawCircle(headX + headRadius * 0.25f, cy - headRadius * 0.3f, Math.Max(1, headRadius * 0.34f), eyePaint);
        }

        // Sharp stinger on wasps / hornets (rear spike).
        if (id is "wasp" or "hornet")
        {
            using var stingPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var sting = new SKPath();
            sting.MoveTo(cx - bodyWidth * 0.95f, cy - bodyHeight * 0.12f);
            sting.LineTo(cx - bodyWidth * 1.3f, cy);
            sting.LineTo(cx - bodyWidth * 0.95f, cy + bodyHeight * 0.12f);
            sting.Close();
            canvas.DrawPath(sting, stingPaint);
        }

        // Stripes on wasps and hornets.
        if (id is "wasp" or "hornet")
        {
            using var stripePaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
            for (int i = -1; i <= 1; i++)
            {
                float sx = cx + i * bodyWidth * 0.22f;
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(sx - bodyWidth * 0.05f, cy - bodyHeight * 0.5f, sx + bodyWidth * 0.05f, cy + bodyHeight * 0.5f), 2), stripePaint);
            }
        }

        using (var antennaPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1, size * 0.05f), IsAntialias = true })
        {
            canvas.DrawLine(headX, cy, headX + size * 0.3f, cy - size * 0.32f, antennaPaint);
            canvas.DrawLine(headX, cy, headX + size * 0.35f, cy - size * 0.2f, antennaPaint);
        }

        // Long, menacing piercing proboscis — the mosquito's weapon. Slightly
        // thicker at the base, tapering to a sharp point, angled down-forward.
        if (id is "mosquito" or "gnat")
        {
            float tipX = headX + size * 0.85f;
            float tipY = cy + size * 0.32f;
            using var probPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var prob = new SKPath();
            prob.MoveTo(headX, cy - size * 0.05f);
            prob.LineTo(headX, cy + size * 0.05f);
            prob.LineTo(tipX, tipY);
            prob.Close();
            canvas.DrawPath(prob, probPaint);
        }
    }

    /// <summary>Vector tower sprite standing on the base — replaces emoji that render as placeholders on some platforms.</summary>
    private static void DrawTowerSprite(SKCanvas canvas, TowerDefinition def, double cx, double cy, float size, double timeMs, byte alpha)
    {
        float x = (float)cx;
        float y = (float)cy;
        switch (def.Id)
        {
            case "candle":
                DrawCandleSprite(canvas, x, y, size, timeMs, alpha);
                break;
            case "zapper":
                DrawZapperSprite(canvas, x, y, size, alpha);
                break;
            case "frog":
                DrawFrogSprite(canvas, x, y, size, alpha);
                break;
            case "gecko":
                DrawGeckoSprite(canvas, x, y, size, alpha);
                break;
            default:
                DrawBottleSprite(canvas, x, y, size, alpha);
                break;
        }
    }

    private static void DrawBottleSprite(SKCanvas canvas, float x, float y, float size, byte alpha)
    {
        var bodyColor = new SKColor(79, 195, 247).WithAlpha(alpha);
        var dark = new SKColor(30, 90, 130).WithAlpha(alpha);
        float w = size * 0.55f;
        float h = size * 1.05f;

        using var bodyPaint = new SKPaint { Color = bodyColor, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x - w * 0.5f, y - h * 0.5f, x + w * 0.5f, y + h * 0.15f), w * 0.3f), bodyPaint);
        using var neckPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x - w * 0.18f, y - h * 0.5f, x + w * 0.18f, y - h * 0.05f), w * 0.12f), neckPaint);
        using var triggerPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x + w * 0.05f, y - h * 0.62f, x + w * 0.32f, y - h * 0.34f), w * 0.1f), triggerPaint);
    }

    private static void DrawCandleSprite(SKCanvas canvas, float x, float y, float size, double timeMs, byte alpha)
    {
        var wax = new SKColor(255, 244, 214).WithAlpha(alpha);
        float w = size * 0.5f;
        float h = size * 0.9f;

        using var waxPaint = new SKPaint { Color = wax, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x - w * 0.5f, y - h * 0.55f, x + w * 0.5f, y + h * 0.35f), w * 0.3f), waxPaint);

        using (var wickPaint = new SKPaint { Color = new SKColor(60, 40, 30).WithAlpha(alpha), Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1, size * 0.05f), IsAntialias = true })
        {
            canvas.DrawLine(x, y - h * 0.55f, x, y - h * 0.78f, wickPaint);
        }

        float flicker = 1f + 0.2f * (float)Math.Sin(timeMs / 60.0);
        float flameRadius = size * 0.18f * flicker;
        using (var flameOuter = new SKPaint { Color = new SKColor(255, 150, 50, 220), Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            canvas.DrawCircle(x, y - h * 0.9f, flameRadius, flameOuter);
        }

        using var flameInner = new SKPaint { Color = new SKColor(255, 230, 120, 240), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(x, y - h * 0.9f, flameRadius * 0.5f, flameInner);
    }

    private static void DrawZapperSprite(SKCanvas canvas, float x, float y, float size, byte alpha)
    {
        var boltColor = new SKColor(255, 241, 118).WithAlpha(alpha);
        var dark = new SKColor(120, 90, 20).WithAlpha(alpha);
        float w = size * 0.5f;
        float h = size * 1.0f;

        using var padPaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x - w * 0.6f, y + h * 0.1f, x + w * 0.6f, y + h * 0.42f), w * 0.2f), padPaint);

        using var boltPath = new SKPath();
        float top = y - h * 0.5f;
        boltPath.MoveTo(x + w * 0.05f, top);
        boltPath.LineTo(x - w * 0.28f, top + h * 0.3f);
        boltPath.LineTo(x - w * 0.02f, top + h * 0.3f);
        boltPath.LineTo(x - w * 0.2f, top + h * 0.62f);
        boltPath.LineTo(x + w * 0.26f, top + h * 0.12f);
        boltPath.LineTo(x + w * 0.02f, top + h * 0.12f);
        boltPath.LineTo(x + w * 0.16f, top - h * 0.05f);
        boltPath.Close();
        using var boltPaint = new SKPaint { Color = boltColor, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawPath(boltPath, boltPaint);
    }

    private static void DrawFrogSprite(SKCanvas canvas, float x, float y, float size, byte alpha)
    {
        var body = new SKColor(129, 199, 132).WithAlpha(alpha);
        var dark = new SKColor(40, 90, 45).WithAlpha(alpha);
        float r = size * 0.5f;

        using var bodyPaint = new SKPaint { Color = body, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x - r, y - r * 0.6f, x + r, y + r * 0.6f), r * 0.7f), bodyPaint);

        using var eyeWhite = new SKPaint { Color = new SKColor(255, 255, 255, alpha), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var eyeDark = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(x - r * 0.4f, y - r * 0.75f, r * 0.28f, eyeWhite);
        canvas.DrawCircle(x + r * 0.4f, y - r * 0.75f, r * 0.28f, eyeWhite);
        canvas.DrawCircle(x - r * 0.35f, y - r * 0.72f, r * 0.13f, eyeDark);
        canvas.DrawCircle(x + r * 0.45f, y - r * 0.72f, r * 0.13f, eyeDark);
    }

    private static void DrawGeckoSprite(SKCanvas canvas, float x, float y, float size, byte alpha)
    {
        var body = new SKColor(186, 104, 200).WithAlpha(alpha);
        var dark = new SKColor(90, 40, 110).WithAlpha(alpha);
        float w = size * 1.6f;
        float h = size * 0.55f;

        using var bodyPaint = new SKPaint { Color = body, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x - w * 0.5f, y - h * 0.5f, x + w * 0.35f, y + h * 0.5f), h * 0.5f), bodyPaint);

        using (var tailPath = new SKPath())
        {
            tailPath.MoveTo(x - w * 0.5f, y);
            tailPath.QuadTo(x - w * 0.75f, y - h * 0.2f, x - w * 0.95f, y + h * 0.15f);
            tailPath.QuadTo(x - w * 0.7f, y + h * 0.35f, x - w * 0.5f, y + h * 0.2f);
            tailPath.Close();
            canvas.DrawPath(tailPath, bodyPaint);
        }

        using var headPaint = new SKPaint { Color = body, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x + w * 0.3f, y - h * 0.4f, x + w * 0.6f, y + h * 0.4f), h * 0.45f), headPaint);

        using var eyePaint = new SKPaint { Color = dark, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(x + w * 0.5f, y - h * 0.2f, size * 0.09f, eyePaint);
    }

    private static void DrawPineTree(SKCanvas canvas, float cx, float cy, float size)
    {
        using var trunkPaint = new SKPaint { Color = new SKColor(90, 62, 38), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRect(new SKRect(cx - size * 0.06f, cy, cx + size * 0.06f, cy + size * 0.28f), trunkPaint);

        using var foliagePaint = new SKPaint { Color = new SKColor(46, 90, 52), Style = SKPaintStyle.Fill, IsAntialias = true };
        for (int i = 0; i < 3; i++)
        {
            float baseY = cy + size * 0.16f - i * size * 0.24f;
            float half = size * 0.42f - i * size * 0.08f;
            float top = baseY - size * 0.4f;
            using var tri = new SKPath();
            tri.MoveTo(cx, top);
            tri.LineTo(cx - half, baseY);
            tri.LineTo(cx + half, baseY);
            tri.Close();
            canvas.DrawPath(tri, foliagePaint);
        }
    }

    private static void DrawTent(SKCanvas canvas, float cx, float cy, float size)
    {
        using (var tri = new SKPath())
        {
            tri.MoveTo(cx, cy - size * 0.72f);
            tri.LineTo(cx - size * 0.6f, cy + size * 0.3f);
            tri.LineTo(cx + size * 0.6f, cy + size * 0.3f);
            tri.Close();
            using var tentPaint = new SKPaint { Color = new SKColor(200, 150, 70), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(tri, tentPaint);
        }

        using (var door = new SKPath())
        {
            door.MoveTo(cx, cy - size * 0.05f);
            door.LineTo(cx - size * 0.2f, cy + size * 0.3f);
            door.LineTo(cx + size * 0.2f, cy + size * 0.3f);
            door.Close();
            using var doorPaint = new SKPaint { Color = new SKColor(90, 62, 38), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(door, doorPaint);
        }
    }

    private static double EffectClock() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static float Distance(SKPoint a, SKPoint b) =>
        (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
