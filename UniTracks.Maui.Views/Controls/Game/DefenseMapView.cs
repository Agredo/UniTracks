using System.Diagnostics;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using UniTracks.Games.TowerDefense;

namespace UniTracks.Maui.Views.Controls.Game;

/// <summary>
/// SkiaSharp-rendered top-down map for the trail defense game. Draws the trail,
/// placed towers with range preview, marching enemies with health bars and homing
/// projectiles. Drives the simulation by executing <see cref="TickCommand"/> from a
/// ~30 fps timer and forwards tile taps via <see cref="TileTappedCommand"/>.
/// </summary>
public class DefenseMapView : SKCanvasView
{
    private static readonly SKColor GrassA = new(46, 71, 52);
    private static readonly SKColor GrassB = new(41, 64, 47);
    private static readonly SKColor PathColor = new(112, 89, 64);
    private static readonly SKColor PathEdge = new(91, 71, 50);

    private readonly Stopwatch tickWatch = new();
    private readonly IDispatcherTimer gameTimer;

    /// <summary>Tile tapped most recently — anchors the ghost preview of the selected tower.</summary>
    private (int X, int Y)? lastTappedTile;

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

    /// <summary>Tower selected in the shop — shown as ghost preview with range circle.</summary>
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

    // Physical pixel size of the canvas, captured during paint — the single source of
    // truth for hit-testing so taps and rendering always agree (no Density math).
    private float canvasWidth;
    private float canvasHeight;

    private float TileSize => Math.Min(canvasWidth / DefensePath.GridWidth, canvasHeight / DefensePath.GridHeight);

    private float OriginX => (canvasWidth - TileSize * DefensePath.GridWidth) / 2f;

    private float OriginY => (canvasHeight - TileSize * DefensePath.GridHeight) / 2f;

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
        if (e.ActionType != SKTouchAction.Pressed || TileTappedCommand is null)
        {
            return;
        }

        int x = (int)Math.Floor((e.Location.X - OriginX) / TileSize);
        int y = (int)Math.Floor((e.Location.Y - OriginY) / TileSize);
        if (x < 0 || x >= DefensePath.GridWidth || y < 0 || y >= DefensePath.GridHeight)
        {
            return;
        }

        lastTappedTile = (x, y);
        var tile = new DefenseTile(x, y);
        if (TileTappedCommand.CanExecute(tile))
        {
            TileTappedCommand.Execute(tile);
        }

        InvalidateSurface();
        e.Handled = true;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvasWidth = e.Info.Width;
        canvasHeight = e.Info.Height;
        canvas.Clear(new SKColor(24, 31, 27));

        if (State is null)
        {
            return;
        }

        DrawTiles(canvas);
        DrawGhost(canvas);
        DrawTowers(canvas);
        DrawEnemies(canvas);
        DrawProjectiles(canvas);
    }

    private void DrawTiles(SKCanvas canvas)
    {
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        float ts = TileSize;

        for (int y = 0; y < DefensePath.GridHeight; y++)
        {
            for (int x = 0; x < DefensePath.GridWidth; x++)
            {
                var rect = CellRect(x, y, inset: 0.5f);
                paint.Color = (x + y) % 2 == 0 ? GrassA : GrassB;
                canvas.DrawRect(rect, paint);

                if (DefensePath.IsPath(x, y))
                {
                    paint.Color = PathColor;
                    canvas.DrawRect(rect, paint);
                }
            }
        }

        // Trail entry and goal markers.
        DrawEmoji(canvas, "🌲", 2, -1, ts * 0.8f);
        DrawEmoji(canvas, "⛺", 6, DefensePath.GridHeight - 1, ts * 0.7f);
    }

    private void DrawGhost(SKCanvas canvas)
    {
        if (GhostTower is null || lastTappedTile is not { } tile || State is null || !State.IsBuildable(tile.X, tile.Y))
        {
            return;
        }

        float ts = TileSize;
        float cx = OriginX + (tile.X + 0.5f) * ts;
        float cy = OriginY + (tile.Y + 0.5f) * ts;

        using var rangePaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 28),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        canvas.DrawCircle(cx, cy, (float)GhostTower.RangeTiles * ts, rangePaint);

        using var ghostPaint = new SKPaint
        {
            Color = SKColor.Parse(GhostTower.ColorHex).WithAlpha(140),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        canvas.DrawCircle(cx, cy, ts * 0.38f, ghostPaint);
        DrawEmoji(canvas, GhostTower.Icon, tile.X, tile.Y, ts * 0.5f, alpha: 180);
    }

    private void DrawTowers(SKCanvas canvas)
    {
        if (State is null)
        {
            return;
        }

        float ts = TileSize;
        foreach (var tower in State.Towers)
        {
            var definition = TowerCatalog.Find(tower.TowerId);
            float cx = OriginX + (tower.X + 0.5f) * ts;
            float cy = OriginY + (tower.Y + 0.5f) * ts;

            using var basePaint = new SKPaint
            {
                Color = SKColor.Parse(definition?.ColorHex ?? "#8BC34A"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            canvas.DrawCircle(cx, cy, ts * 0.38f, basePaint);

            using var ringPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 90),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
            };
            canvas.DrawCircle(cx, cy, ts * 0.38f, ringPaint);

            DrawEmoji(canvas, definition?.Icon ?? "🗼", tower.X, tower.Y, ts * 0.5f);
        }
    }

    private void DrawEnemies(SKCanvas canvas)
    {
        if (State is null)
        {
            return;
        }

        float ts = TileSize;
        foreach (var enemy in State.Enemies)
        {
            var (ex, ey) = enemy.Position;
            float px = OriginX + (float)ex * ts;
            float py = OriginY + (float)ey * ts;

            using var textPaint = new SKPaint
            {
                TextSize = ts * 0.55f,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
            };
            canvas.DrawText(enemy.Definition.Icon, px, py + ts * 0.2f, textPaint);

            // Health bar above the enemy (only when damaged).
            if (enemy.Hp < enemy.MaxHp)
            {
                float barWidth = ts * 0.6f;
                float barLeft = px - barWidth / 2f;
                float barTop = py - ts * 0.42f;
                using var backPaint = new SKPaint { Color = new SKColor(0, 0, 0, 120), Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(barLeft, barTop, barLeft + barWidth, barTop + ts * 0.08f), backPaint);

                float fraction = Math.Max(0, (float)enemy.Hp / enemy.MaxHp);
                using var hpPaint = new SKPaint { Color = new SKColor(77, 231, 144), Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(barLeft, barTop, barLeft + barWidth * fraction, barTop + ts * 0.08f), hpPaint);
            }
        }
    }

    private void DrawProjectiles(SKCanvas canvas)
    {
        if (State is null)
        {
            return;
        }

        float ts = TileSize;
        foreach (var projectile in State.Projectiles)
        {
            using var paint = new SKPaint
            {
                Color = SKColor.Parse(projectile.ColorHex),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            canvas.DrawCircle(
                OriginX + (float)projectile.X * ts,
                OriginY + (float)projectile.Y * ts,
                ts * 0.09f,
                paint);
        }
    }

    private SKRect CellRect(int x, int y, float inset)
    {
        float ts = TileSize;
        return new SKRect(
            OriginX + x * ts + inset,
            OriginY + y * ts + inset,
            OriginX + (x + 1) * ts - inset,
            OriginY + (y + 1) * ts - inset);
    }

    private void DrawEmoji(SKCanvas canvas, string emoji, int tileX, int tileY, float size, byte alpha = 255)
    {
        float ts = TileSize;
        using var paint = new SKPaint
        {
            TextSize = size,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true,
            Color = SKColors.White.WithAlpha(alpha),
        };
        canvas.DrawText(emoji, OriginX + (tileX + 0.5f) * ts, OriginY + (tileY + 0.72f) * ts, paint);
    }
}
