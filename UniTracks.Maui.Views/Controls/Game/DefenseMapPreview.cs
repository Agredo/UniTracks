using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using UniTracks.Games.TowerDefense;

namespace UniTracks.Maui.Views.Controls.Game;

/// <summary>
/// A tiny isometric preview of a <see cref="DefenseMap"/> for the selection screen.
/// Renders only the grass/dirt tiles plus the trail entry/exit markers — no towers,
/// enemies, effects, pan/zoom or touch input. Static: paints once per size change.
/// </summary>
public class DefenseMapPreview : SKCanvasView
{
    public static readonly BindableProperty MapProperty = BindableProperty.Create(
        nameof(Map), typeof(DefenseMap), typeof(DefenseMapPreview),
        propertyChanged: static (b, _, _) => ((DefenseMapPreview)b).InvalidateSurface());

    public DefenseMap? Map
    {
        get => (DefenseMap?)GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    private static readonly SKColor SkyTop = new(18, 26, 22);
    private static readonly SKColor SkyBottom = new(30, 44, 35);

    private readonly SKPath sidePath = new();
    private readonly SKPath topPath = new();
    private readonly SKPaint sidePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint topPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float width = e.Info.Width;
        float height = e.Info.Height;

        try
        {
            using var bgPaint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(0, height),
                    new[] { SkyTop, SkyBottom }, null, SKShaderTileMode.Clamp),
            };
            canvas.DrawRect(new SKRect(0, 0, width, height), bgPaint);

            if (Map is null)
            {
                return;
            }

            DrawBoard(canvas, width, height);
        }
        catch (Exception ex)
        {
            CrashLog.Write($"Map preview paint failed: {ex}");
        }
    }

    private void DrawBoard(SKCanvas canvas, float width, float height)
    {
        var map = Map!;
        double halfSpan = (map.GridWidth + map.GridHeight) / 2.0;
        double tileW = width * 0.96 / halfSpan;
        double tileH = tileW / 2.0;
        double originX = width / 2.0;
        double originY = (height - halfSpan * tileH) / 2.0;

        // Painter's algorithm: back row first so front tiles overlap correctly.
        for (int y = 0; y < map.GridHeight; y++)
        {
            for (int x = 0; x < map.GridWidth; x++)
            {
                DrawTile(canvas, map, x, y, originX, originY, tileW, tileH);
            }
        }

        DrawPineTree(canvas,
            (float)ScreenX(map.Entry.X, map.Entry.Y, originX, tileW),
            (float)ScreenY(map.Entry.X, map.Entry.Y, originY, tileH),
            (float)(tileW * 0.42));
        DrawTent(canvas,
            (float)ScreenX(map.Exit.X, map.Exit.Y, originX, tileW),
            (float)ScreenY(map.Exit.X, map.Exit.Y, originY, tileH),
            (float)(tileW * 0.42));
    }

    private void DrawTile(SKCanvas canvas, DefenseMap map, int x, int y, double originX, double originY, double tileW, double tileH)
    {
        int hash = (x * 31 + y * 17) % 5;
        byte g = (byte)(84 + hash * 5);
        var top = new SKColor(58, g, 64);
        var side = new SKColor(42, 62, 47);
        bool decorateForest = false;

        switch (map.TileKind(x, y))
        {
            case DefenseTileKind.Path:
                top = new SKColor(112, 89, 64);
                side = new SKColor(91, 71, 50);
                break;
            case DefenseTileKind.Water:
                top = new SKColor(40, 78, 116);
                side = new SKColor(26, 56, 90);
                break;
            case DefenseTileKind.Forest:
                top = new SKColor(48, 74, 54);
                side = new SKColor(34, 54, 40);
                decorateForest = true;
                break;
        }

        float cx = (float)ScreenX(x + 0.5, y + 0.5, originX, tileW);
        float cy = (float)ScreenY(x + 0.5, y + 0.5, originY, tileH);
        float hw = (float)(tileW / 2.0);
        float hh = (float)(tileH / 2.0);

        sidePaint.Color = side;
        sidePath.Rewind();
        sidePath.MoveTo(cx - hw, cy);
        sidePath.LineTo(cx, cy + hh);
        sidePath.LineTo(cx + hw, cy);
        sidePath.LineTo(cx + hw, cy + hh * 0.25f);
        sidePath.LineTo(cx, cy + hh * 1.25f);
        sidePath.LineTo(cx - hw, cy + hh * 0.25f);
        sidePath.Close();
        canvas.DrawPath(sidePath, sidePaint);

        topPaint.Color = top;
        topPath.Rewind();
        topPath.MoveTo(cx, cy - hh);
        topPath.LineTo(cx + hw, cy);
        topPath.LineTo(cx, cy + hh);
        topPath.LineTo(cx - hw, cy);
        topPath.Close();
        canvas.DrawPath(topPath, topPaint);

        if (decorateForest)
        {
            DrawPineTree(canvas, cx, cy - hh * 0.35f, hw * 0.9f);
        }
    }

    private static double ScreenX(double x, double y, double originX, double tileW) => originX + (x - y) * tileW / 2.0;
    private static double ScreenY(double x, double y, double originY, double tileH) => originY + (x + y) * tileH / 2.0;

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
}
