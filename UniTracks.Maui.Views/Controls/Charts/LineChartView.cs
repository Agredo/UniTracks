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

    /// <summary>
    /// Optional per-series missing-value markers. Used by comparison paces, where an unavailable
    /// kilometre is represented by a non-positive placeholder. Normal speed and altitude charts
    /// keep zero as a valid measurement.
    /// </summary>
    public static readonly BindableProperty MissingValuesProperty = BindableProperty.Create(
        nameof(MissingValues), typeof(IList<IList<bool>>), typeof(LineChartView), null,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public static readonly BindableProperty PrimaryMissingValuesProperty = BindableProperty.Create(
        nameof(PrimaryMissingValues), typeof(IList<bool>), typeof(LineChartView), null,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public static readonly BindableProperty SecondaryMissingValuesProperty = BindableProperty.Create(
        nameof(SecondaryMissingValues), typeof(IList<bool>), typeof(LineChartView), null,
        propertyChanged: (b, _, _) => ((LineChartView)b).Invalidate());

    public LineChartView()
    {
        Drawable = new LineChartDrawable(this);
    }

    public IList<IList<bool>>? MissingValues
    {
        get => (IList<IList<bool>>?)GetValue(MissingValuesProperty);
        set => SetValue(MissingValuesProperty, value);
    }

    public IList<bool>? PrimaryMissingValues
    {
        get => (IList<bool>?)GetValue(PrimaryMissingValuesProperty);
        set => SetValue(PrimaryMissingValuesProperty, value);
    }

    public IList<bool>? SecondaryMissingValues
    {
        get => (IList<bool>?)GetValue(SecondaryMissingValuesProperty);
        set => SetValue(SecondaryMissingValuesProperty, value);
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

    private bool HasMissingValues =>
        PrimaryMissingValues?.Contains(true) == true
        || SecondaryMissingValues?.Contains(true) == true;

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

            var primaryRaw = Smooth(owner.PrimaryValues ?? new List<double>(), owner.PrimaryMissingValues);
            var secondaryRaw = Smooth(owner.SecondaryValues ?? new List<double>(), owner.SecondaryMissingValues);

            // One scale for both series when they share a unit, so equal values land on equal
            // heights and the gap between the curves is the actual difference.
            double sharedMin = 0;
            double sharedMax = 0;
            var measured = owner.SharedScale
                ? MeasuredValues(primaryRaw, owner.PrimaryMissingValues)
                    .Concat(MeasuredValues(secondaryRaw, owner.SecondaryMissingValues))
                    .ToList()
                : new List<double>();
            if (owner.SharedScale && measured.Count > 0)
            {
                sharedMin = measured.Min();
                sharedMax = measured.Max();
            }

            var secondary = Normalize(secondaryRaw, sharedMin, sharedMax, owner.SecondaryMissingValues);
            if (secondary is { Count: > 1 })
            {
                var secondaryPath = BuildSeriesPath(secondary, owner.SecondaryMissingValues, width, height);
                canvas.StrokeColor = owner.SecondaryColor;
                canvas.StrokeSize = 1.5f;
                canvas.StrokeDashPattern = new[] { 4f, 3f };
                canvas.DrawPath(secondaryPath);
                canvas.StrokeDashPattern = null;
            }

            var primary = Normalize(primaryRaw, sharedMin, sharedMax, owner.PrimaryMissingValues);
            if (primary is not { Count: > 1 })
            {
                return;
            }

            if (owner.HasMissingValues)
            {
                DrawAreaSegments(canvas, primary, owner.PrimaryMissingValues, width, height);
            }
            else
            {
                DrawArea(canvas, primary, width, height);
            }

            canvas.StrokeColor = owner.LineColor;
            canvas.StrokeSize = 2.5f;
            canvas.DrawPath(BuildSeriesPath(primary, owner.PrimaryMissingValues, width, height));
        }

        /// <summary>
        /// Draws every series as a plain line, all scaled to the same min/max so the curves can be
        /// read against each other. No fill — with more than two curves the area would only obscure
        /// the ones underneath.
        /// </summary>
        private void DrawSeries(ICanvas canvas, IReadOnlyList<ComparisonCurve> series, float width, float height)
        {
            var smoothed = series
                .Select((curve, index) => Smooth(
                    curve.Values.ToList(),
                    owner.MissingValues is { } missing && index < missing.Count ? missing[index] : null))
                .ToList();

            var all = smoothed.SelectMany(values => values).ToList();
            if (all.Count == 0)
            {
                return;
            }

            // Missing comparison kilometres must not drag the shared pace scale to zero.
            var measured = MeasuredValues(smoothed, owner.MissingValues);
            double sharedMin = measured.Count > 0 ? measured.Min() : all.Min();
            double sharedMax = measured.Count > 0 ? measured.Max() : all.Max();

            for (int index = 0; index < series.Count; index++)
            {
                var missing = owner.MissingValues is { } masks && index < masks.Count ? masks[index] : null;
                var normalized = Normalize(smoothed[index], sharedMin, sharedMax, missing);
                if (normalized is not { Count: > 1 })
                {
                    continue;
                }

                canvas.StrokeColor = Color.FromArgb(series[index].ColorHex);
                canvas.StrokeSize = 2.5f;
                canvas.DrawPath(BuildSeriesPath(normalized, missing, width, height));
            }
        }

        /// <summary>Starts a new segment after an unmeasured value instead of joining across the gap.</summary>
        private static PathF BuildSeriesPath(
            IReadOnlyList<float> normalized,
            IList<bool>? missing,
            float width,
            float height)
        {
            var path = new PathF();
            bool drawing = false;

            for (int i = 0; i < normalized.Count; i++)
            {
                if (IsMissing(missing, i))
                {
                    drawing = false;
                    continue;
                }

                float x = Padding + width * i / (normalized.Count - 1);
                float y = Padding + height * (1f - normalized[i]);
                if (drawing)
                {
                    path.LineTo(x, y);
                }
                else
                {
                    path.MoveTo(x, y);
                    drawing = true;
                }
            }

            return path;
        }

        private void DrawArea(
            ICanvas canvas,
            IReadOnlyList<float> normalized,
            float width,
            float height)
        {
            var fill = new PathF();
            for (int index = 0; index < normalized.Count; index++)
            {
                float x = Padding + width * index / (normalized.Count - 1);
                float y = Padding + height * (1f - normalized[index]);
                if (index == 0)
                {
                    fill.MoveTo(x, y);
                }
                else
                {
                    fill.LineTo(x, y);
                }
            }

            fill.LineTo(Padding + width, Padding + height);
            fill.LineTo(Padding, Padding + height);
            fill.Close();
            canvas.FillColor = owner.FillColor;
            canvas.FillPath(fill);
        }

        private void DrawAreaSegments(
            ICanvas canvas,
            IReadOnlyList<float> normalized,
            IList<bool>? missing,
            float width,
            float height)
        {
            canvas.FillColor = owner.FillColor;

            for (int start = 0; start < normalized.Count - 1; start++)
            {
                if (IsMissing(missing, start))
                {
                    continue;
                }

                int end = start + 1;
                while (end < normalized.Count && !IsMissing(missing, end))
                {
                    end++;
                }
                end--;

                if (end <= start)
                {
                    continue;
                }

                var fill = new PathF();
                float firstX = Padding + width * start / (normalized.Count - 1);
                float lastX = Padding + width * end / (normalized.Count - 1);
                fill.MoveTo(firstX, Padding + height);
                fill.LineTo(firstX, Padding + height * (1f - normalized[start]));

                for (int index = start + 1; index <= end; index++)
                {
                    float x = Padding + width * index / (normalized.Count - 1);
                    fill.LineTo(x, Padding + height * (1f - normalized[index]));
                }

                fill.LineTo(lastX, Padding + height);
                fill.Close();
                canvas.FillPath(fill);
                start = end - 1;
            }
        }

        private List<float>? Normalize(
            IList<double>? values,
            double sharedMin = 0,
            double sharedMax = 0,
            IList<bool>? missing = null)
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
            else if (sharedMin > 0)
            {
                min = sharedMin;
                max = sharedMin;
            }
            else
            {
                var measuredValues = MeasuredValues(values, missing);
                if (measuredValues.Count == 0)
                {
                    return null;
                }

                min = measuredValues.Min();
                max = measuredValues.Max();
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

        private static List<double> Smooth(IList<double> values, IList<bool>? missing = null)
        {
            var result = new List<double>(values.Count);
            int half = SmoothingWindow / 2;
            for (int i = 0; i < values.Count; i++)
            {
                // Missing pace values are left untouched and excluded from neighbours' windows.
                // Ordinary speed and altitude charts do not pass a mask: zero stays a real value.
                if (IsMissing(missing, i))
                {
                    result.Add(0);
                    continue;
                }

                int from = Math.Max(0, i - half);
                int to = Math.Min(values.Count - 1, i + half);
                double sum = 0;
                int count = 0;
                for (int j = from; j <= to; j++)
                {
                    if (IsMissing(missing, j))
                    {
                        continue;
                    }

                    sum += values[j];
                    count++;
                }

                result.Add(count > 0 ? sum / count : 0);
            }

            return result;
        }

        private static bool IsMissing(IList<bool>? missing, int index) =>
            missing is not null && index < missing.Count && missing[index];

        private static List<double> MeasuredValues(
            IList<double> values,
            IList<bool>? missing = null)
        {
            var result = new List<double>(values.Count);
            for (int index = 0; index < values.Count; index++)
            {
                if (!IsMissing(missing, index))
                {
                    result.Add(values[index]);
                }
            }

            return result;
        }

        private static IReadOnlyList<double> MeasuredValues(
            IReadOnlyList<List<double>> series,
            IList<IList<bool>>? masks)
        {
            var values = new List<double>();
            for (int index = 0; index < series.Count; index++)
            {
                values.AddRange(MeasuredValues(
                    series[index],
                    masks is not null && index < masks.Count ? masks[index] : null));
            }

            return values;
        }
    }
}
