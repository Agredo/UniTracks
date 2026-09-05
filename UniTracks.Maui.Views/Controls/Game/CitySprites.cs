using SkiaSharp;
using UniTracks.Games.CityBuilder;

namespace UniTracks.Maui.Views.Controls.Game;

/// <summary>
/// Procedural vector sprites for every building in the <see cref="BuildingCatalog"/>.
/// All shapes are drawn relative to the tile center (sx, sy) with the tile width as
/// the unit scale, so they stay crisp at any zoom level.
/// </summary>
public sealed class CitySprites
{
    private static readonly SKColor RoofRed = new(178, 74, 58);
    private static readonly SKColor RoofDark = new(96, 84, 78);
    private static readonly SKColor WallCream = new(238, 224, 200);
    private static readonly SKColor WallWhite = new(240, 240, 236);
    private static readonly SKColor WoodBrown = new(122, 86, 58);
    private static readonly SKColor LeafGreen = new(72, 148, 82);
    private static readonly SKColor PineGreen = new(48, 112, 76);
    private static readonly SKColor WaterBlue = new(88, 162, 216);
    private static readonly SKColor StoneGray = new(158, 160, 164);
    private static readonly SKColor WindowWarm = new(255, 214, 120);

    /// <summary>Draws a building sprite. <paramref name="scale"/> drives the drop-in bounce.</summary>
    public void Draw(SKCanvas canvas, string buildingId, float sx, float sy, float tileWidth, float scale, float night)
    {
        canvas.Save();
        canvas.Translate(sx, sy);
        canvas.Scale(scale, scale);
        canvas.Translate(-sx, -sy);

        switch (buildingId)
        {
            case "flowerbed": DrawFlowerbed(canvas, sx, sy, tileWidth); break;
            case "tree": DrawTree(canvas, sx, sy, tileWidth, false); break;
            case "pine": DrawTree(canvas, sx, sy, tileWidth, true); break;
            case "fountain": DrawFountain(canvas, sx, sy, tileWidth); break;
            case "house": DrawHouse(canvas, sx, sy, tileWidth, RoofRed, WallCream, 0.52f); break;
            case "playground": DrawPlayground(canvas, sx, sy, tileWidth); break;
            case "cafe": DrawCafe(canvas, sx, sy, tileWidth); break;
            case "shop": DrawShop(canvas, sx, sy, tileWidth); break;
            case "villa": DrawHouse(canvas, sx, sy, tileWidth, RoofDark, WallWhite, 0.68f); break;
            case "school": DrawSchool(canvas, sx, sy, tileWidth); break;
            case "hospital": DrawHospital(canvas, sx, sy, tileWidth); break;
            default: DrawHouse(canvas, sx, sy, tileWidth, RoofRed, WallCream, 0.52f); break;
        }

        canvas.Restore();

        if (night > 0.15f)
        {
            DrawNightLights(canvas, buildingId, sx, sy, tileWidth, night);
        }
    }

    /// <summary>Semi-transparent preview of a building on the selected tile.</summary>
    public void DrawGhost(SKCanvas canvas, string buildingId, float sx, float sy, float tileWidth)
    {
        canvas.Save();
        using var layer = new SKPaint { Color = new SKColor(255, 255, 255, 110) };
        canvas.SaveLayer(layer);
        Draw(canvas, buildingId, sx, sy, tileWidth, 1f, 0f);
        canvas.Restore();
        canvas.Restore();
    }

    private static void DrawFlowerbed(SKCanvas canvas, float sx, float sy, float w)
    {
        float r = w * 0.075f;
        using var soilPaint = Fill(new SKColor(110, 84, 60));
        canvas.DrawOval(sx, sy, w * 0.30f, w * 0.16f, soilPaint);

        SKColor[] petals = { new(235, 105, 130), new(244, 200, 92), new(190, 130, 220), new(235, 105, 130), new(244, 200, 92) };
        for (int i = 0; i < 5; i++)
        {
            float fx = sx - w * 0.22f + i * w * 0.11f;
            float fy = sy - r * 0.6f - (i % 2) * r * 0.7f;
            using var petal = Fill(petals[i]);
            canvas.DrawCircle(fx, fy, r, petal);
            using var center = Fill(new SKColor(250, 240, 180));
            canvas.DrawCircle(fx, fy, r * 0.4f, center);
        }
    }

    private static void DrawTree(SKCanvas canvas, float sx, float sy, float w, bool pine)
    {
        using var trunkPaint = Fill(WoodBrown);
        float trunkW = w * 0.07f;
        canvas.DrawRect(sx - trunkW / 2, sy - w * 0.22f, trunkW, w * 0.22f, trunkPaint);

        if (pine)
        {
            using var paint = Fill(PineGreen);
            for (int i = 0; i < 3; i++)
            {
                float baseY = sy - w * (0.18f + i * 0.13f);
                float half = w * (0.24f - i * 0.055f);
                using var path = new SKPath();
                path.MoveTo(sx, baseY - w * 0.20f);
                path.LineTo(sx + half, baseY);
                path.LineTo(sx - half, baseY);
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }
        else
        {
            using var paint = Fill(LeafGreen);
            using var dark = Fill(new SKColor(56, 122, 68));
            canvas.DrawCircle(sx, sy - w * 0.34f, w * 0.20f, paint);
            canvas.DrawCircle(sx - w * 0.13f, sy - w * 0.26f, w * 0.14f, dark);
            canvas.DrawCircle(sx + w * 0.13f, sy - w * 0.27f, w * 0.15f, paint);
        }
    }

    private static void DrawFountain(SKCanvas canvas, float sx, float sy, float w)
    {
        using var stone = Fill(StoneGray);
        using var water = Fill(WaterBlue);
        using var waterLight = Fill(new SKColor(168, 214, 245, 190));

        // Basin.
        canvas.DrawOval(sx, sy, w * 0.30f, w * 0.15f, stone);
        canvas.DrawOval(sx, sy - w * 0.02f, w * 0.25f, w * 0.115f, water);

        // Center pillar + upper bowl.
        canvas.DrawRect(sx - w * 0.03f, sy - w * 0.22f, w * 0.06f, w * 0.18f, stone);
        canvas.DrawOval(sx, sy - w * 0.22f, w * 0.13f, w * 0.06f, stone);
        canvas.DrawOval(sx, sy - w * 0.23f, w * 0.10f, w * 0.045f, waterLight);

        // Animated water shimmer arcs.
        float phase = (float)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1200 / 1200.0);
        using var shimmer = new SKPaint
        {
            Color = new SKColor(220, 240, 255, (byte)(140 * (1 - phase))),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
        };
        float shimmerR = w * (0.05f + 0.16f * phase);
        canvas.DrawOval(sx, sy - w * 0.02f, shimmerR, shimmerR * 0.45f, shimmer);
    }

    private static void DrawHouse(SKCanvas canvas, float sx, float sy, float w, SKColor roof, SKColor wall, float size)
    {
        float bw = w * size;
        float bh = w * size * 0.62f;
        float left = sx - bw / 2;
        float top = sy - bh;

        using var wallPaint = Fill(wall);
        using var roofPaint = Fill(roof);
        using var wood = Fill(WoodBrown);

        // Walls.
        canvas.DrawRect(left, top, bw, bh, wallPaint);

        // Gable roof.
        using (var path = new SKPath())
        {
            path.MoveTo(left - bw * 0.08f, top);
            path.LineTo(sx, top - bh * 0.55f);
            path.LineTo(left + bw * 1.08f, top);
            path.Close();
            canvas.DrawPath(path, roofPaint);
        }

        // Door + windows.
        canvas.DrawRect(sx - bw * 0.09f, sy - bh * 0.42f, bw * 0.18f, bh * 0.42f, wood);
        using var windowPaint = Fill(new SKColor(150, 190, 215));
        canvas.DrawRect(left + bw * 0.14f, top + bh * 0.2f, bw * 0.17f, bh * 0.22f, windowPaint);
        canvas.DrawRect(left + bw * 0.66f, top + bh * 0.2f, bw * 0.17f, bh * 0.22f, windowPaint);
    }

    private static void DrawPlayground(SKCanvas canvas, float sx, float sy, float w)
    {
        using var frame = new SKPaint { Color = new SKColor(210, 120, 70), Style = SKPaintStyle.Stroke, StrokeWidth = w * 0.03f, IsAntialias = true, StrokeCap = SKStrokeCap.Round };
        using var slidePaint = Fill(new SKColor(90, 150, 220));
        using var sandPaint = Fill(new SKColor(222, 200, 150));

        canvas.DrawOval(sx, sy, w * 0.30f, w * 0.14f, sandPaint);

        // Swing A-frame.
        float topY = sy - w * 0.34f;
        canvas.DrawLine(sx - w * 0.16f, sy - w * 0.04f, sx - w * 0.08f, topY, frame);
        canvas.DrawLine(sx + w * 0.02f, sy - w * 0.04f, sx - w * 0.08f, topY, frame);
        canvas.DrawLine(sx - w * 0.08f, topY, sx + w * 0.16f, topY, frame);

        // Swing seat swaying gently.
        float sway = (float)Math.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 480.0) * w * 0.02f;
        float seatX = sx + w * 0.06f + sway;
        canvas.DrawLine(sx + w * 0.06f, topY, seatX, topY + w * 0.16f, frame);
        using var seat = Fill(WoodBrown);
        canvas.DrawRect(seatX - w * 0.035f, topY + w * 0.16f, w * 0.07f, w * 0.02f, seat);

        // Slide.
        using (var path = new SKPath())
        {
            path.MoveTo(sx + w * 0.20f, sy - w * 0.26f);
            path.LineTo(sx + w * 0.30f, sy - w * 0.02f);
            path.LineTo(sx + w * 0.22f, sy - w * 0.02f);
            path.Close();
            canvas.DrawPath(path, slidePaint);
        }
    }

    private static void DrawCafe(SKCanvas canvas, float sx, float sy, float w)
    {
        float bw = w * 0.56f;
        float bh = bw * 0.5f;
        float left = sx - bw / 2;
        float top = sy - bh;

        using var wallPaint = Fill(new SKColor(226, 205, 172));
        canvas.DrawRect(left, top, bw, bh, wallPaint);

        // Striped awning.
        int stripes = 5;
        for (int i = 0; i < stripes; i++)
        {
            var color = i % 2 == 0 ? new SKColor(196, 84, 74) : new SKColor(245, 238, 224);
            using var paint = Fill(color);
            canvas.DrawRect(left + bw * i / stripes, top - w * 0.05f, bw / stripes, w * 0.07f, paint);
        }

        // Window + door.
        using var windowPaint = Fill(new SKColor(150, 190, 215));
        canvas.DrawRect(left + bw * 0.12f, top + bh * 0.28f, bw * 0.3f, bh * 0.4f, windowPaint);
        using var wood = Fill(WoodBrown);
        canvas.DrawRect(left + bw * 0.6f, top + bh * 0.28f, bw * 0.24f, bh * 0.72f, wood);

        // Coffee cup sign.
        using var signPaint = Fill(new SKColor(120, 84, 58));
        canvas.DrawRoundRect(sx + bw * 0.28f, top - w * 0.16f, w * 0.14f, w * 0.09f, 3, 3, signPaint);
    }

    private static void DrawShop(SKCanvas canvas, float sx, float sy, float w)
    {
        float bw = w * 0.6f;
        float bh = bw * 0.55f;
        float left = sx - bw / 2;
        float top = sy - bh;

        using var wallPaint = Fill(new SKColor(198, 214, 228));
        using var roofPaint = Fill(new SKColor(88, 98, 110));
        canvas.DrawRect(left, top, bw, bh, wallPaint);
        canvas.DrawRect(left - bw * 0.05f, top - w * 0.045f, bw * 1.1f, w * 0.05f, roofPaint);

        // Big shop window with goods.
        using var glass = Fill(new SKColor(170, 205, 225));
        canvas.DrawRect(left + bw * 0.1f, top + bh * 0.3f, bw * 0.52f, bh * 0.55f, glass);
        using var goods = Fill(new SKColor(230, 160, 90));
        canvas.DrawRect(left + bw * 0.16f, top + bh * 0.62f, bw * 0.12f, bh * 0.23f, goods);
        using var goods2 = Fill(new SKColor(120, 180, 120));
        canvas.DrawRect(left + bw * 0.34f, top + bh * 0.55f, bw * 0.12f, bh * 0.3f, goods2);

        using var wood = Fill(WoodBrown);
        canvas.DrawRect(left + bw * 0.72f, top + bh * 0.3f, bw * 0.2f, bh * 0.7f, wood);

        // Sign.
        using var sign = Fill(new SKColor(77, 150, 130));
        canvas.DrawRoundRect(sx - bw * 0.22f, top - w * 0.13f, bw * 0.44f, w * 0.08f, 4, 4, sign);
    }

    private static void DrawSchool(SKCanvas canvas, float sx, float sy, float w)
    {
        DrawHouse(canvas, sx, sy, w, new SKColor(150, 92, 60), new SKColor(238, 205, 140), 0.66f);

        // Clock on the gable.
        using var clockFace = Fill(WallWhite);
        using var clockHands = new SKPaint { Color = new SKColor(70, 70, 80), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
        float cy = sy - w * 0.66f * 0.62f - w * 0.10f;
        canvas.DrawCircle(sx, cy, w * 0.055f, clockFace);
        canvas.DrawLine(sx, cy, sx, cy - w * 0.032f, clockHands);
        canvas.DrawLine(sx, cy, sx + w * 0.022f, cy, clockHands);

        // Flag.
        using var pole = new SKPaint { Color = StoneGray, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f, IsAntialias = true };
        float flagTop = sy - w * 0.62f;
        canvas.DrawLine(sx + w * 0.22f, sy - w * 0.30f, sx + w * 0.22f, flagTop, pole);
        float wave = (float)Math.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 350.0) * w * 0.015f;
        using var flag = Fill(new SKColor(210, 80, 80));
        using var flagPath = new SKPath();
        flagPath.MoveTo(sx + w * 0.22f, flagTop);
        flagPath.LineTo(sx + w * 0.33f + wave, flagTop + w * 0.025f);
        flagPath.LineTo(sx + w * 0.22f, flagTop + w * 0.05f);
        flagPath.Close();
        canvas.DrawPath(flagPath, flag);
    }

    private static void DrawHospital(SKCanvas canvas, float sx, float sy, float w)
    {
        float bw = w * 0.68f;
        float bh = bw * 0.66f;
        float left = sx - bw / 2;
        float top = sy - bh;

        using var wallPaint = Fill(WallWhite);
        using var roofPaint = Fill(new SKColor(120, 130, 140));
        canvas.DrawRect(left, top, bw, bh, wallPaint);
        canvas.DrawRect(left - bw * 0.04f, top - w * 0.04f, bw * 1.08f, w * 0.045f, roofPaint);

        // Red cross.
        using var cross = Fill(new SKColor(214, 70, 70));
        float cw = w * 0.045f;
        float cl = w * 0.14f;
        canvas.DrawRect(sx - cw / 2, top + bh * 0.12f, cw, cl, cross);
        canvas.DrawRect(sx - cl / 2, top + bh * 0.12f + cl / 2 - cw / 2, cl, cw, cross);

        // Window grid.
        using var windowPaint = Fill(new SKColor(160, 200, 220));
        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                canvas.DrawRect(left + bw * (0.14f + c * 0.27f), top + bh * (0.45f + r * 0.28f), bw * 0.16f, bh * 0.18f, windowPaint);
            }
        }
    }

    /// <summary>Warm window glow at night — the cozy payoff of the day cycle.</summary>
    private static void DrawNightLights(SKCanvas canvas, string buildingId, float sx, float sy, float w, float night)
    {
        bool hasWindows = buildingId is "house" or "villa" or "cafe" or "shop" or "school" or "hospital";
        if (!hasWindows)
        {
            return;
        }

        byte alpha = (byte)(200 * night);
        using var glow = new SKPaint
        {
            Color = WindowWarm.WithAlpha(alpha),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6),
        };
        canvas.DrawCircle(sx, sy - w * 0.22f, w * 0.16f, glow);
    }

    private static SKPaint Fill(SKColor color) => new() { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
}
