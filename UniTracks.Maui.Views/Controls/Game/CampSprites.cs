using SkiaSharp;
using UniTracks.Games.BaseCamp;

namespace UniTracks.Maui.Views.Controls.Game;

/// <summary>
/// Procedural vector sprites for the base-camp scene: an alpine backdrop plus one drawing per
/// camp module. Everything is drawn from primitives, so the game ships without any image assets
/// and every module visibly grows with its level.
/// </summary>
public class CampSprites
{
    private static readonly SKColor SkyTop = new(120, 176, 226);
    private static readonly SKColor SkyBottom = new(206, 230, 244);
    private static readonly SKColor PeakFar = new(148, 164, 186);
    private static readonly SKColor PeakNear = new(112, 128, 152);
    private static readonly SKColor Snow = new(248, 250, 253);
    private static readonly SKColor Rock = new(122, 122, 126);
    private static readonly SKColor Grass = new(120, 164, 96);
    private static readonly SKColor GrassDark = new(92, 134, 74);
    private static readonly SKColor CanvasCream = new(236, 226, 202);
    private static readonly SKColor CanvasShadow = new(198, 186, 162);
    private static readonly SKColor Wood = new(124, 88, 58);
    private static readonly SKColor WoodDark = new(94, 66, 44);
    private static readonly SKColor Metal = new(126, 132, 140);
    private static readonly SKColor FlameOuter = new(240, 142, 48);
    private static readonly SKColor FlameInner = new(252, 214, 96);
    private static readonly SKColor CrateTan = new(196, 158, 104);
    private static readonly SKColor RoofSlate = new(96, 92, 104);

    /// <summary>Alpine backdrop: gradient sky, two mountain ranges and the plateau the camp sits on.</summary>
    public void DrawBackdrop(SKCanvas canvas, SKRect bounds, double time)
    {
        using var sky = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(bounds.MidX, bounds.Top),
                new SKPoint(bounds.MidX, bounds.Bottom),
                new[] { SkyTop, SkyBottom },
                null,
                SKShaderTileMode.Clamp),
        };
        canvas.DrawRect(bounds, sky);

        DrawSun(canvas, bounds, time);
        DrawClouds(canvas, bounds, time);

        // Far range sits higher and hazier than the near range, which gives the depth.
        DrawRange(canvas, bounds, 0.34f, 0.30f, PeakFar, 3, 0.0);
        DrawRange(canvas, bounds, 0.50f, 0.24f, PeakNear, 4, 1.7);

        float groundY = bounds.Bottom - bounds.Height * 0.30f;
        using var plateau = Fill(Grass);
        canvas.DrawRect(new SKRect(bounds.Left, groundY, bounds.Right, bounds.Bottom), plateau);

        using var ridge = Fill(GrassDark);
        using var ridgePath = new SKPath();
        ridgePath.MoveTo(bounds.Left, groundY + 6);
        for (float x = bounds.Left; x <= bounds.Right; x += bounds.Width / 12f)
        {
            ridgePath.LineTo(x, groundY + 6 - (float)(Math.Sin(x * 0.02) * 7));
        }

        ridgePath.LineTo(bounds.Right, groundY + 26);
        ridgePath.LineTo(bounds.Left, groundY + 26);
        ridgePath.Close();
        canvas.DrawPath(ridgePath, ridge);
    }

    /// <summary>Ground line the camp modules are placed on.</summary>
    public static float GroundLine(SKRect bounds) => bounds.Bottom - bounds.Height * 0.30f;

    /// <summary>One module drawing. <paramref name="level"/> 0 means "not built" (nothing is drawn).</summary>
    public void DrawModule(SKCanvas canvas, string moduleId, int level, float x, float groundY, float unit, double time)
    {
        if (level <= 0)
        {
            return;
        }

        switch (moduleId)
        {
            case CampCatalog.TentId:
                DrawTent(canvas, x, groundY, unit, level);
                break;
            case CampCatalog.KitchenId:
                DrawCampfire(canvas, x, groundY, unit, level, time);
                break;
            case CampCatalog.CellarId:
                DrawStockpile(canvas, x, groundY, unit, level);
                break;
            case CampCatalog.MapId:
                DrawMapTable(canvas, x, groundY, unit, level);
                break;
            case CampCatalog.RadioId:
                DrawRadioMast(canvas, x, groundY, unit, level, time);
                break;
            case CampCatalog.HutId:
                DrawHut(canvas, x, groundY, unit, level);
                break;
            case CampCatalog.FlagId:
                DrawSummitFlag(canvas, x, groundY, unit, time);
                break;
        }
    }

    /// <summary>Dashed marker for an empty slot; the caller draws the faded sprite on top.</summary>
    public void DrawGhostSlot(SKCanvas canvas, float x, float groundY, float unit)
    {
        using var dashed = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 110),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1.5f, unit * 0.02f),
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { unit * 0.06f, unit * 0.05f }, 0),
        };

        var rect = new SKRect(x - unit * 0.30f, groundY - unit * 0.44f, x + unit * 0.30f, groundY - unit * 0.04f);
        canvas.DrawRoundRect(rect, unit * 0.06f, unit * 0.06f, dashed);
    }

    private static void DrawSun(SKCanvas canvas, SKRect bounds, double time)
    {
        float cx = bounds.Left + bounds.Width * 0.82f;
        float cy = bounds.Top + bounds.Height * 0.16f;
        float radius = bounds.Width * 0.05f;
        float pulse = (float)(1 + 0.04 * Math.Sin(time * 0.8));

        using var glow = Fill(new SKColor(255, 240, 190, 70));
        canvas.DrawCircle(cx, cy, radius * 1.9f * pulse, glow);

        using var core = Fill(new SKColor(255, 246, 214));
        canvas.DrawCircle(cx, cy, radius, core);
    }

    private static void DrawClouds(SKCanvas canvas, SKRect bounds, double time)
    {
        using var cloud = Fill(new SKColor(255, 255, 255, 168));
        for (int i = 0; i < 3; i++)
        {
            float speed = 8 + i * 4;
            float width = bounds.Width * (0.20f + i * 0.05f);
            float travel = bounds.Width + width * 2;
            float x = bounds.Left - width + (float)((time * speed + i * 320) % travel);
            float y = bounds.Top + bounds.Height * (0.10f + i * 0.07f);

            canvas.DrawCircle(x, y, width * 0.16f, cloud);
            canvas.DrawCircle(x + width * 0.20f, y - width * 0.06f, width * 0.20f, cloud);
            canvas.DrawCircle(x + width * 0.44f, y, width * 0.15f, cloud);
        }
    }

    private static void DrawRange(SKCanvas canvas, SKRect bounds, float heightFactor, float topFactor, SKColor color, int peaks, double phase)
    {
        float baseY = bounds.Bottom - bounds.Height * heightFactor;
        float topY = bounds.Top + bounds.Height * topFactor;
        float span = bounds.Width / peaks;

        using var paint = Fill(color);
        using var snow = Fill(Snow);

        for (int i = 0; i < peaks; i++)
        {
            float apexX = bounds.Left + span * (i + 0.5f) + (float)Math.Sin(i * 1.3 + phase) * span * 0.12f;
            float apexY = topY + (float)Math.Sin(i * 0.9 + phase) * bounds.Height * 0.04f;

            using var path = new SKPath();
            path.MoveTo(apexX - span * 0.72f, baseY);
            path.LineTo(apexX, apexY);
            path.LineTo(apexX + span * 0.72f, baseY);
            path.Close();
            canvas.DrawPath(path, paint);

            // Snow cap: a smaller triangle hugging the apex.
            float capY = apexY + (baseY - apexY) * 0.26f;
            using var cap = new SKPath();
            cap.MoveTo(apexX - span * 0.19f, capY);
            cap.LineTo(apexX, apexY);
            cap.LineTo(apexX + span * 0.19f, capY);
            cap.Close();
            canvas.DrawPath(cap, snow);
        }
    }

    private static void DrawTent(SKCanvas canvas, float x, float groundY, float unit, int level)
    {
        float size = unit * (0.52f + level * 0.10f);

        // Extra tents are pitched behind the main one as the tentplatz grows.
        for (int i = level - 1; i >= 1; i--)
        {
            float offset = i * unit * 0.34f * (i % 2 == 0 ? -1 : 1);
            DrawSingleTent(canvas, x + offset, groundY - unit * 0.02f, size * 0.66f, CanvasShadow, CanvasCream, WoodDark);
        }

        DrawSingleTent(canvas, x, groundY, size, CanvasCream, CanvasCream, Wood);
    }

    private static void DrawSingleTent(SKCanvas canvas, float x, float groundY, float size, SKColor wall, SKColor highlight, SKColor frame)
    {
        using var shadow = Fill(new SKColor(0, 0, 0, 40));
        canvas.DrawOval(new SKRect(x - size * 0.58f, groundY - size * 0.10f, x + size * 0.58f, groundY + size * 0.10f), shadow);

        using var body = Fill(wall);
        using var accent = Fill(highlight);
        using var pole = Fill(frame);

        using var path = new SKPath();
        path.MoveTo(x - size * 0.55f, groundY);
        path.LineTo(x, groundY - size);
        path.LineTo(x + size * 0.55f, groundY);
        path.Close();
        canvas.DrawPath(path, body);

        // Front flap: a darker wedge in the middle of the tent.
        using var flap = new SKPath();
        flap.MoveTo(x - size * 0.16f, groundY);
        flap.LineTo(x, groundY - size * 0.72f);
        flap.LineTo(x + size * 0.16f, groundY);
        flap.Close();
        canvas.DrawPath(flap, pole);

        using var ridge = Fill(new SKColor(255, 255, 255, 120));
        canvas.DrawRect(new SKRect(x - size * 0.03f, groundY - size * 0.99f, x + size * 0.03f, groundY - size * 0.86f), accent);
    }

    private static void DrawCampfire(SKCanvas canvas, float x, float groundY, float unit, int level, double time)
    {
        float size = unit * 0.30f;

        // Ring of stones.
        using var stone = Fill(Rock);
        for (int i = 0; i < 7; i++)
        {
            double angle = i * Math.PI * 2 / 7;
            float sx = x + (float)Math.Cos(angle) * size * 1.25f;
            float sy = groundY - size * 0.12f + (float)Math.Sin(angle) * size * 0.34f;
            canvas.DrawCircle(sx, sy, size * 0.17f, stone);
        }

        // Logs.
        using var log = Fill(WoodDark);
        canvas.DrawRoundRect(new SKRect(x - size * 0.8f, groundY - size * 0.34f, x + size * 0.8f, groundY - size * 0.12f), size * 0.1f, size * 0.1f, log);
        canvas.DrawRoundRect(new SKRect(x - size * 0.7f, groundY - size * 0.5f, x + size * 0.7f, groundY - size * 0.28f), size * 0.1f, size * 0.1f, log);

        // Flickering flame, taller with every kitchen level.
        float flicker = (float)(1 + 0.12 * Math.Sin(time * 6.0) + 0.06 * Math.Sin(time * 11.3));
        float height = size * (1.5f + level * 0.28f) * flicker;

        using var outer = Fill(new SKColor(FlameOuter.Red, FlameOuter.Green, FlameOuter.Blue, 200));
        using var inner = Fill(FlameInner);
        using var flamePath = new SKPath();
        flamePath.MoveTo(x - size * 0.55f, groundY - size * 0.3f);
        flamePath.QuadTo(x - size * 0.2f, groundY - height * 0.6f, x, groundY - height);
        flamePath.QuadTo(x + size * 0.2f, groundY - height * 0.6f, x + size * 0.55f, groundY - size * 0.3f);
        flamePath.Close();
        canvas.DrawPath(flamePath, outer);

        using var innerPath = new SKPath();
        innerPath.MoveTo(x - size * 0.3f, groundY - size * 0.3f);
        innerPath.QuadTo(x - size * 0.1f, groundY - height * 0.5f, x, groundY - height * 0.68f);
        innerPath.QuadTo(x + size * 0.1f, groundY - height * 0.5f, x + size * 0.3f, groundY - size * 0.3f);
        innerPath.Close();
        canvas.DrawPath(innerPath, inner);

        DrawSmoke(canvas, x, groundY - height, size, time);
    }

    private static void DrawSmoke(SKCanvas canvas, float x, float topY, float size, double time)
    {
        for (int i = 0; i < 3; i++)
        {
            double phase = (time * 0.5 + i * 0.33) % 1.0;
            float y = topY - (float)phase * size * 3.2f;
            float radius = size * (0.22f + (float)phase * 0.42f);
            byte alpha = (byte)Math.Max(0, 110 - phase * 110);
            float drift = (float)Math.Sin(time * 0.9 + i) * size * 0.5f;

            using var smoke = Fill(new SKColor(230, 230, 232, alpha));
            canvas.DrawCircle(x + drift, y, radius, smoke);
        }
    }

    private static void DrawStockpile(SKCanvas canvas, float x, float groundY, float unit, int level)
    {
        float crate = unit * 0.30f;

        using var shadow = Fill(new SKColor(0, 0, 0, 38));
        canvas.DrawOval(new SKRect(x - unit * 0.7f, groundY - unit * 0.08f, x + unit * 0.7f, groundY + unit * 0.08f), shadow);

        // The cellar is a cool pit with crates stacked next to it — more levels, more crates.
        using var pit = Fill(new SKColor(72, 66, 60));
        canvas.DrawOval(new SKRect(x - unit * 0.42f, groundY - unit * 0.20f, x + unit * 0.42f, groundY - unit * 0.02f), pit);
        using var rim = Fill(Wood);
        using var rimStroke = new SKPaint { Color = WoodDark, Style = SKPaintStyle.Stroke, StrokeWidth = unit * 0.03f, IsAntialias = true };
        canvas.DrawOval(new SKRect(x - unit * 0.44f, groundY - unit * 0.24f, x + unit * 0.44f, groundY - unit * 0.04f), rimStroke);

        int crates = 1 + level;
        for (int i = 0; i < crates; i++)
        {
            int row = i / 3;
            int column = i % 3;
            float cx = x + unit * 0.60f + column * crate * 0.92f;
            float cy = groundY - crate * 0.5f - row * crate * 0.9f;
            DrawCrate(canvas, cx, cy, crate);
        }
    }

    private static void DrawCrate(SKCanvas canvas, float cx, float cy, float size)
    {
        using var body = Fill(CrateTan);
        using var edge = new SKPaint { Color = WoodDark, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1f, size * 0.06f), IsAntialias = true };

        var rect = new SKRect(cx - size * 0.5f, cy - size * 0.5f, cx + size * 0.5f, cy + size * 0.5f);
        canvas.DrawRect(rect, body);
        canvas.DrawRect(rect, edge);
        canvas.DrawLine(cx - size * 0.5f, cy - size * 0.5f, cx + size * 0.5f, cy + size * 0.5f, edge);
        canvas.DrawLine(cx + size * 0.5f, cy - size * 0.5f, cx - size * 0.5f, cy + size * 0.5f, edge);
    }

    private static void DrawMapTable(SKCanvas canvas, float x, float groundY, float unit, int level)
    {
        float width = unit * (0.62f + level * 0.10f);
        float topY = groundY - unit * 0.36f;

        using var leg = Fill(WoodDark);
        canvas.DrawRect(new SKRect(x - width * 0.42f, topY, x - width * 0.32f, groundY), leg);
        canvas.DrawRect(new SKRect(x + width * 0.32f, topY, x + width * 0.42f, groundY), leg);

        using var top = Fill(Wood);
        canvas.DrawRoundRect(new SKRect(x - width * 0.5f, topY - unit * 0.08f, x + width * 0.5f, topY + unit * 0.04f), unit * 0.02f, unit * 0.02f, top);

        // Paper map with a dashed route on it.
        using var paper = Fill(new SKColor(244, 240, 224));
        var paperRect = new SKRect(x - width * 0.34f, topY - unit * 0.22f, x + width * 0.34f, topY - unit * 0.04f);
        canvas.DrawRect(paperRect, paper);

        using var route = new SKPaint
        {
            Color = new SKColor(198, 82, 74),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1.2f, unit * 0.02f),
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { unit * 0.05f, unit * 0.04f }, 0),
        };
        canvas.DrawLine(paperRect.Left + unit * 0.04f, paperRect.Bottom - unit * 0.04f, paperRect.Right - unit * 0.05f, paperRect.Top + unit * 0.05f, route);

        // Markers on the map for each level.
        using var pin = Fill(new SKColor(198, 82, 74));
        for (int i = 0; i < level; i++)
        {
            float px = paperRect.Left + paperRect.Width * (0.22f + i * 0.26f);
            float py = paperRect.Bottom - unit * 0.04f - paperRect.Height * (0.25f + i * 0.22f);
            canvas.DrawCircle(px, py, unit * 0.035f, pin);
        }
    }

    private static void DrawRadioMast(SKCanvas canvas, float x, float groundY, float unit, int level, double time)
    {
        float height = unit * (0.95f + level * 0.30f);
        float topY = groundY - height;

        using var mast = Fill(Metal);
        using var guy = new SKPaint { Color = new SKColor(120, 120, 124, 160), Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1f, unit * 0.012f), IsAntialias = true };

        // Guy wires from the mast top to the ground keep it readable at small sizes.
        canvas.DrawLine(x, topY, x - height * 0.42f, groundY, guy);
        canvas.DrawLine(x, topY, x + height * 0.42f, groundY, guy);

        canvas.DrawRoundRect(new SKRect(x - unit * 0.035f, topY, x + unit * 0.035f, groundY), unit * 0.02f, unit * 0.02f, mast);

        // Cross bars and the antenna dish.
        for (int i = 1; i <= 3; i++)
        {
            float y = groundY - height * i / 3.6f;
            float arm = unit * (0.18f - i * 0.03f);
            canvas.DrawRoundRect(new SKRect(x - arm, y - unit * 0.014f, x + arm, y + unit * 0.014f), unit * 0.014f, unit * 0.014f, mast);
        }

        using var dish = Fill(new SKColor(210, 212, 216));
        canvas.DrawOval(new SKRect(x - unit * 0.16f, topY - unit * 0.14f, x + unit * 0.16f, topY + unit * 0.10f), dish);

        // Pulsing signal arcs — the visible payoff of the radio.
        for (int i = 0; i < 3; i++)
        {
            double phase = (time * 0.7 + i * 0.33) % 1.0;
            float radius = unit * (0.20f + (float)phase * 0.55f);
            byte alpha = (byte)Math.Max(0, 190 - phase * 190);
            using var wave = new SKPaint
            {
                Color = new SKColor(120, 210, 255, alpha),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(1.4f, unit * 0.022f),
                IsAntialias = true,
            };
            canvas.DrawArc(new SKRect(x - radius, topY - radius, x + radius, topY + radius), -70, 140, false, wave);
        }
    }

    private static void DrawHut(SKCanvas canvas, float x, float groundY, float unit, int level)
    {
        float width = unit * 0.95f;
        float height = unit * 0.62f;

        using var shadow = Fill(new SKColor(0, 0, 0, 44));
        canvas.DrawOval(new SKRect(x - width * 0.62f, groundY - unit * 0.07f, x + width * 0.62f, groundY + unit * 0.07f), shadow);

        using var wall = Fill(Wood);
        using var plank = new SKPaint { Color = WoodDark, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1f, unit * 0.014f), IsAntialias = true };
        var body = new SKRect(x - width * 0.5f, groundY - height, x + width * 0.5f, groundY);
        canvas.DrawRect(body, wall);
        for (int i = 1; i < 4; i++)
        {
            float y = body.Top + body.Height * i / 4f;
            canvas.DrawLine(body.Left, y, body.Right, y, plank);
        }

        // Wide overhanging roof with a stone chimney.
        using var roof = Fill(RoofSlate);
        using var roofPath = new SKPath();
        roofPath.MoveTo(x - width * 0.66f, body.Top + unit * 0.02f);
        roofPath.LineTo(x, body.Top - height * 0.55f);
        roofPath.LineTo(x + width * 0.66f, body.Top + unit * 0.02f);
        roofPath.Close();
        canvas.DrawPath(roofPath, roof);

        using var chimney = Fill(Rock);
        canvas.DrawRect(new SKRect(x + width * 0.22f, body.Top - height * 0.42f, x + width * 0.36f, body.Top - height * 0.05f), chimney);

        // Warm, lit window — the hut is the "everything is fine" signal.
        using var glow = Fill(new SKColor(255, 226, 150, 110));
        canvas.DrawCircle(x, groundY - height * 0.55f, unit * 0.22f, glow);
        using var window = Fill(new SKColor(255, 214, 120));
        canvas.DrawRoundRect(new SKRect(x - unit * 0.09f, groundY - height * 0.66f, x + unit * 0.09f, groundY - height * 0.44f), unit * 0.02f, unit * 0.02f, window);
    }

    private static void DrawSummitFlag(SKCanvas canvas, float x, float groundY, float unit, double time)
    {
        float height = unit * 0.9f;
        float topY = groundY - height;

        using var pole = Fill(new SKColor(226, 226, 230));
        canvas.DrawRoundRect(new SKRect(x - unit * 0.025f, topY, x + unit * 0.025f, groundY), unit * 0.012f, unit * 0.012f, pole);

        float wave = (float)Math.Sin(time * 2.4) * unit * 0.06f;
        using var cloth = Fill(new SKColor(214, 92, 78));
        using var flag = new SKPath();
        flag.MoveTo(x + unit * 0.025f, topY + unit * 0.02f);
        flag.LineTo(x + unit * 0.40f + wave, topY + unit * 0.14f);
        flag.LineTo(x + unit * 0.025f, topY + unit * 0.30f);
        flag.Close();
        canvas.DrawPath(flag, cloth);
    }

    private static SKPaint Fill(SKColor color) => new() { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
}
