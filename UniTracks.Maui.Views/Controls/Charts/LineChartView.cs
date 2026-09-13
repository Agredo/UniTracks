using System.Collections;
using UniTracks.Models.Comparison;

namespace UniTracks.Maui.Views.Controls.Charts;

/// <summary>
/// Reusable line/area chart for one or two numeric series (e.g. speed and altitude
/// profiles along a trip). Pure GraphicsView + IDrawable; each series is normalized
/// to its own min/max so different units can share one plot. Series are smoothed
/// with a small moving average before rendering.
/// </summary>
public class LineChartView : GraphicsView
{
    public static readonly BindableProperty PrimaryValuesProperty = BindableProperty.Create(
        nameof(PrimaryValues), typeof(IList<double>), typeof(LineChartView), null,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public static readonly BindableProperty SecondaryValuesProperty = BindableProperty.Create(
        nameof(SecondaryValues), typeof(IList<double>), typeof(LineChartView), null,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public static readonly BindableProperty LineColorProperty = BindableProperty.Create(
        nameof(LineColor), typeof(Color), typeof(LineChartView), Color.FromArgb("#4DE790"),
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor), typeof(Color), typeof(LineChartView), Color.FromArgb("#334DE790"),
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public static readonly BindableProperty SecondaryColorProperty = BindableProperty.Create(
        nameof(SecondaryColor), typeof(Color), typeof(LineChartView), Color.FromArgb("#6B7A6D"),
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    /// <summary>
    /// Scales both series against one shared min/max instead of normalizing each to its own range.
    /// Off by default, which keeps the existing behaviour of overlaying two different units (speed
    /// and altitude). Required when the two series are the same unit and must be comparable —
    /// e.g. the pace of two trips, where per-series scaling would draw both as the same curve.
    /// </summary>
    public static readonly BindableProperty SharedScaleProperty = BindableProperty.Create(
        nameof(SharedScale), typeof(bool), typeof(LineChartView), false,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    /// <summary>
    /// Flips the value axis so a low value is drawn high. Needed for pace, where the fast trip has
    /// the small number and should be the one on top.
    /// </summary>
    public static readonly BindableProperty InvertYProperty = BindableProperty.Create(
        nameof(InvertY), typeof(bool), typeof(LineChartView), false,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    /// <summary>
    /// Three or more same-unit series, each drawn in its own colour on one shared scale. Takes
    /// precedence over <see cref="PrimaryValues"/>/<see cref="SecondaryValues"/>, which stay the
    /// two-series overlay case. Used when more than two trips are compared at once.
    /// </summary>
    public static readonly BindableProperty SeriesProperty = BindableProperty.Create(
        nameof(Series), typeof(IReadOnlyList<ComparisonCurve>), typeof(LineChartView), null,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public LineChartView()
    {
        Drawable = new LineChartDrawable(this);
    }

    public IReadOnlyList<ComparisonCurve>? Series
    {
        get => (IReadOnlyList<ComparisonCurve>?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public IList<double>? PrimaryValues
    {
        get => (IList<double>?)GetValue(PrimaryValuesProperty);
        set => SetValue(PrimaryValuesProperty, value);
    }

    public IList<double>? SecondaryValues
    {
        get => (IList<double>?)GetValue(SecondaryValuesProperty);
        set => SetValue(SecondaryValuesProperty, value);
    }

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public Color SecondaryColor
    {
        get => (Color)GetValue(SecondaryColorProperty);
        set => SetValue(SecondaryColorProperty, value);
    }

    public bool SharedScale
    {
        get => (bool)GetValue(SharedScaleProperty);
        set => SetValue(SharedScaleProperty, value);
    }

    public bool InvertY
    {
        get => (bool)GetValue(InvertYProperty);
        set => SetValue(InvertYProperty, value);
    }

    private sealed class LineChartDrawable : IDrawable
    {
        private const float Padding = 4f;
        private const int SmoothingWindow = 5;

        private readonly LineChartView owner;

        public LineChartDrawable(LineChartView owner)
        {
            this.owner = owner;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float width = dirtyRect.Width - Padding * 2;
            float height = dirtyRect.Height - Padding * 2;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (owner.Series is { Count: > 0 } series)
            {
                DrawSeries(canvas, series, width, height);
                return;
            }

            var primaryRaw = owner.PrimaryValues is null ? new List<double>() : Smooth(owner.PrimaryValues);
            var secondaryRaw = owner.SecondaryValues is null ? new List<double>() : Smooth(owner.SecondaryValues);

            // One scale for both series when they share a unit, so equal values land on equal
            // heights and the gap between the curves is the actual difference.
            double sharedMin = 0;
            double sharedMax = 0;
            if (owner.SharedScale)
            {
                var all = primaryRaw.Concat(secondaryRaw).ToList();
                if (all.Count > 0)
                {
                    sharedMin = all.Min();
                    sharedMax = all.Max();
                }
            }

            var secondary = Normalize(secondaryRaw, sharedMin, sharedMax);
            if (secondary is { Count: > 1 })
            {
                var secondaryPath = BuildPath(secondary, width, height);
                canvas.StrokeColor = owner.SecondaryColor;
                canvas.StrokeSize = 1.5f;
                canvas.StrokeDashPattern = new[] { 4f, 3f };
                canvas.DrawPath(secondaryPath);
                canvas.StrokeDashPattern = null;
            }

            var primary = Normalize(primaryRaw, sharedMin, sharedMax);
            if (primary is not { Count: > 1 })
            {
                return;
            }

            var linePath = BuildPath(primary, width, height);

            var fillPath = BuildPath(primary, width, height);
            fillPath.LineTo(Padding + width, Padding + height);
            fillPath.LineTo(Padding, Padding + height);
            fillPath.Close();

            canvas.FillColor = owner.FillColor;
            canvas.FillPath(fillPath);

            canvas.StrokeColor = owner.LineColor;
            canvas.StrokeSize = 2.5f;
            canvas.DrawPath(linePath);
        }

        /// <summary>
        /// Draws every series as a plain line, all scaled to the same min/max so the curves can be
        /// read against each other. No fill — with more than two curves the area would only obscure
        /// the ones underneath.
        /// </summary>
        private void DrawSeries(ICanvas canvas, IReadOnlyList<ComparisonCurve> series, float width, float height)
        {
            var smoothed = series
                .Select(curve => curve.Values.Count == 0 ? null : Smooth(curve.Values.ToList()))
                .ToList();

            var all = smoothed.Where(values => values is not null).SelectMany(values => values!).ToList();
            if (all.Count == 0)
            {
                return;
            }

            // A missing kilometre is stored as 0; letting those zeros into the pool would squash all
            // curves onto the top edge, so the scale is taken from the real values only.
            var measured = all.Where(value => value > 0).ToList();
            double sharedMin = measured.Count > 0 ? measured.Min() : all.Min();
            double sharedMax = measured.Count > 0 ? measured.Max() : all.Max();

            for (int index = 0; index < series.Count; index++)
            {
                var normalized = Normalize(smoothed[index], sharedMin, sharedMax);
                if (normalized is not { Count: > 1 })
                {
                    continue;
                }

                canvas.StrokeColor = Color.FromArgb(series[index].ColorHex);
                canvas.StrokeSize = 2.5f;
                canvas.DrawPath(BuildPath(normalized, width, height));
            }
        }

        private static PathF BuildPath(IReadOnlyList<float> normalized, float width, float height)
        {
            var path = new PathF();
            for (int i = 0; i < normalized.Count; i++)
            {
                float x = Padding + width * i / (normalized.Count - 1);
                float y = Padding + height * (1f - normalized[i]);
                if (i == 0)
                {
                    path.MoveTo(x, y);
                }
                else
                {
                    path.LineTo(x, y);
                }
            }

            return path;
        }

        private List<float>? Normalize(IList<double>? values, double sharedMin = 0, double sharedMax = 0)
        {
            if (values is not { Count: > 0 })
            {
                return null;
            }

            double min;
            double max;

            if (sharedMax > sharedMin)
            {
                min = sharedMin;
                max = sharedMax;
            }
            else
            {
                min = values.Min();
                max = values.Max();
            }

            double range = Math.Max(max - min, 0.0001);

            var normalized = values.Select(v => (float)((v - min) / range)).ToList();

            if (InvertY)
            {
                for (int i = 0; i < normalized.Count; i++)
                {
                    normalized[i] = 1f - normalized[i];
                }
            }

            return normalized;
        }

        private bool InvertY => owner.InvertY;

        private static List<double> Smooth(IList<double> values)
        {
            var result = new List<double>(values.Count);
            int half = SmoothingWindow / 2;
            for (int i = 0; i < values.Count; i++)
            {
                int from = Math.Max(0, i - half);
                int to = Math.Min(values.Count - 1, i + half);
                double sum = 0;
                for (int j = from; j <= to; j++)
                {
                    sum += values[j];
                }

                result.Add(sum / (to - from + 1));
            }

            return result;
        }
    }
}
