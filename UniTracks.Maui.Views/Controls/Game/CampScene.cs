using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using UniTracks.Games.BaseCamp;

namespace UniTracks.Maui.Views.Controls.Game;

/// <summary>
/// SkiaSharp-rendered base camp for the idle game. The scene is a fixed alpine layout with one
/// slot per camp module: an unbuilt module appears as a dashed, faded preview, and every level
/// makes its sprite grow. Ambient animation (clouds, fire flicker, smoke, radio waves, the
/// waving summit flag) runs on the same 30 fps dispatcher timer the other games use, and is
/// paused while the page is off screen.
/// </summary>
public class CampScene : SKCanvasView
{
    /// <summary>Fixed slot per module: x as a fraction of the canvas width, y offset in units above the ground line.</summary>
    private static readonly (string ModuleId, float X, float YOffset)[] Slots =
    {
        // The hut sits further up the slope — it is the last thing you build.
        (CampCatalog.HutId, 0.50f, -0.42f),
        (CampCatalog.TentId, 0.17f, 0.00f),
        (CampCatalog.KitchenId, 0.37f, 0.05f),
        (CampCatalog.CellarId, 0.58f, 0.00f),
        (CampCatalog.MapId, 0.76f, 0.05f),
        (CampCatalog.RadioId, 0.91f, 0.00f),
        (CampCatalog.FlagId, 0.07f, -1.05f),
    };

    private readonly CampSprites sprites = new();
    private readonly IDispatcherTimer animationTimer;

    private float canvasWidth;
    private float canvasHeight;
    private double elapsed;
    private string? selectedModuleId;

    public CampScene()
    {
        EnableTouchEvents = true;
        Touch += OnTouch;

        // ~30 fps ambient animation loop. Started only once the view is in the visual tree and
        // stopped when it leaves, so navigating away does not keep redrawing in the background.
        animationTimer = Dispatcher.CreateTimer();
        animationTimer.Interval = TimeSpan.FromMilliseconds(33);
        animationTimer.Tick += (_, _) =>
        {
            elapsed += 0.033;
            InvalidateSurface();
        };
        Loaded += (_, _) => animationTimer.Start();
        Unloaded += (_, _) => animationTimer.Stop();
    }

    public static readonly BindableProperty CampProperty = BindableProperty.Create(
        nameof(Camp), typeof(CampState), typeof(CampScene),
        propertyChanged: static (b, _, _) => ((CampScene)b).InvalidateSurface());

    public CampState? Camp
    {
        get => (CampState?)GetValue(CampProperty);
        set => SetValue(CampProperty, value);
    }

    public static readonly BindableProperty ModuleTappedCommandProperty = BindableProperty.Create(
        nameof(ModuleTappedCommand), typeof(ICommand), typeof(CampScene));

    /// <summary>Invoked with the module id when a built module is tapped.</summary>
    public ICommand? ModuleTappedCommand
    {
        get => (ICommand?)GetValue(ModuleTappedCommandProperty);
        set => SetValue(ModuleTappedCommandProperty, value);
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvasWidth = info.Width;
        canvasHeight = info.Height;

        var bounds = new SKRect(0, 0, info.Width, info.Height);
        sprites.DrawBackdrop(canvas, bounds, elapsed);

        var camp = Camp;
        if (camp is null)
        {
            return;
        }

        float groundY = CampSprites.GroundLine(bounds);
        float unit = info.Width / 6.5f;

        // Back-to-front, so the hut up the slope never covers the tents in front of it.
        foreach (var slot in Slots.OrderBy(s => s.YOffset))
        {
            float x = bounds.Left + info.Width * slot.X;
            float y = groundY + unit * slot.YOffset;
            int level = camp.LevelOf(slot.ModuleId);

            if (level <= 0)
            {
                sprites.DrawGhostSlot(canvas, x, y, unit);

                // Faded preview of what the slot will become, so the goal is always visible.
                using var fade = new SKPaint { Color = new SKColor(255, 255, 255, 70), IsAntialias = true };
                canvas.SaveLayer(fade);
                sprites.DrawModule(canvas, slot.ModuleId, 1, x, y, unit, elapsed);
                canvas.Restore();
                continue;
            }

            sprites.DrawModule(canvas, slot.ModuleId, level, x, y, unit, elapsed);

            if (selectedModuleId == slot.ModuleId)
            {
                DrawSelectionRing(canvas, x, y, unit);
            }

            var definition = CampCatalog.Find(slot.ModuleId);
            if (definition is not null && camp.CanUpgrade(definition))
            {
                DrawUpgradeBubble(canvas, x, y, unit, elapsed);
            }
        }
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType != SKTouchAction.Released)
        {
            return;
        }

        e.Handled = true;

        var camp = Camp;
        if (camp is null || canvasWidth <= 0)
        {
            return;
        }

        float groundY = canvasHeight - canvasHeight * 0.30f;
        float unit = canvasWidth / 6.5f;

        // Nearest built slot within one unit — a forgiving hit box, since the sprites are small.
        string? hit = null;
        float best = float.MaxValue;
        foreach (var slot in Slots)
        {
            if (camp.LevelOf(slot.ModuleId) <= 0)
            {
                continue;
            }

            float x = canvasWidth * slot.X;
            float y = groundY + unit * slot.YOffset;
            float distance = MathF.Sqrt((x - e.Location.X) * (x - e.Location.X) + (y - e.Location.Y) * (y - e.Location.Y));
            if (distance < unit && distance < best)
            {
                best = distance;
                hit = slot.ModuleId;
            }
        }

        if (hit is null)
        {
            return;
        }

        selectedModuleId = hit;
        InvalidateSurface();

        if (ModuleTappedCommand?.CanExecute(hit) == true)
        {
            ModuleTappedCommand.Execute(hit);
        }
    }

    private static void DrawSelectionRing(SKCanvas canvas, float x, float y, float unit)
    {
        using var ring = new SKPaint
        {
            Color = new SKColor(255, 236, 168, 220),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(2f, unit * 0.035f),
            IsAntialias = true,
        };

        canvas.DrawOval(new SKRect(x - unit * 0.55f, y - unit * 0.14f, x + unit * 0.55f, y + unit * 0.14f), ring);
    }

    /// <summary>Small bouncing badge over a module whose next level is affordable right now.</summary>
    private static void DrawUpgradeBubble(SKCanvas canvas, float x, float y, float unit, double time)
    {
        float bob = (float)Math.Sin(time * 3.0) * unit * 0.04f;
        float cx = x;
        float cy = y - unit * 0.86f + bob;
        float radius = unit * 0.12f;

        using var bubble = new SKPaint { Color = new SKColor(72, 186, 122, 235), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(cx, cy, radius, bubble);

        using var plus = new SKPaint
        {
            Color = new SKColor(255, 255, 255),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1.6f, unit * 0.03f),
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true,
        };
        canvas.DrawLine(cx - radius * 0.45f, cy, cx + radius * 0.45f, cy, plus);
        canvas.DrawLine(cx, cy - radius * 0.45f, cx, cy + radius * 0.45f, plus);
    }
}
