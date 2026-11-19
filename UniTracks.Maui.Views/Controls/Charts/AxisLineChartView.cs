using System.Collections;
using System.Globalization;
using UniTracks.Models.Comparison;
using UniTracks.Services.Comparison;

namespace UniTracks.Maui.Views.Controls.Charts;

/// <summary>
/// Detailed line chart with labelled axes: horizontal grid lines with Y tick labels
/// (formatted via <see cref="YFormat"/>, unit via <see cref="YUnit"/>) and evenly
/// spaced X labels (e.g. elapsed time). Pure GraphicsView + IDrawable, themed via
/// bindable colors. Values are smoothed with a small moving average before rendering.
/// </summary>
public class AxisLineChartView : GraphicsView
{
    public static readonly BindableProperty ValuesProperty = BindableProperty.Create(
        nameof(Values), typeof(IList<double>), typeof(AxisLineChartView), null,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty XLabelsProperty = BindableProperty.Create(
        nameof(XLabels), typeof(IList<string>), typeof(AxisLineChartView), null,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty YFormatProperty = BindableProperty.Create(
        nameof(YFormat), typeof(string), typeof(AxisLineChartView), "0",
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty YUnitProperty = BindableProperty.Create(
        nameof(YUnit), typeof(string), typeof(AxisLineChartView), string.Empty,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty LineColorProperty = BindableProperty.Create(
        nameof(LineColor), typeof(Color), typeof(AxisLineChartView), Color.FromArgb("#4DE790"),
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor), typeof(Color), typeof(AxisLineChartView), Color.FromArgb("#334DE790"),
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty LabelColorProperty = BindableProperty.Create(
        nameof(LabelColor), typeof(Color), typeof(AxisLineChartView), Color.FromArgb("#6B7A6D"),
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty GridColorProperty = BindableProperty.Create(
        nameof(GridColor), typeof(Color), typeof(AxisLineChartView), Color.FromArgb("#226B7A6D"),
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty SeriesProperty = BindableProperty.Create(
        nameof(Series), typeof(IReadOnlyList<ComparisonCurve>), typeof(AxisLineChartView), null,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty MissingValuesProperty = BindableProperty.Create(
        nameof(MissingValues), typeof(IList<IList<bool>>), typeof(AxisLineChartView), null,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty InvertYProperty = BindableProperty.Create(
        nameof(InvertY), typeof(bool), typeof(AxisLineChartView), false,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty IsPaceScaleProperty = BindableProperty.Create(
        nameof(IsPaceScale), typeof(bool), typeof(AxisLineChartView), false,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public static readonly BindableProperty SmoothValuesProperty = BindableProperty.Create(
        nameof(SmoothValues), typeof(bool), typeof(AxisLineChartView), true,
        propertyChanged: (b, _, _) => ((AxisLineChartView)b).Invalidate());

    public AxisLineChartView()
    {
        Drawable = new AxisLineChartDrawable(this);
    }

    public IList<double>? Values
    {
        get => (IList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IList<string>? XLabels
    {
        get => (IList<string>?)GetValue(XLabelsProperty);
        set => SetValue(XLabelsProperty, value);
    }

    public string YFormat
    {
        get => (string)GetValue(YFormatProperty);
        set => SetValue(YFormatProperty, value);
    }

    public string YUnit
    {
        get => (string)GetValue(YUnitProperty);
        set => SetValue(YUnitProperty, value);
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

    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    public Color GridColor
    {
        get => (Color)GetValue(GridColorProperty);
        set => SetValue(GridColorProperty, value);
    }

    public IReadOnlyList<ComparisonCurve>? Series
    {
        get => (IReadOnlyList<ComparisonCurve>?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public IList<IList<bool>>? MissingValues
    {
        get => (IList<IList<bool>>?)GetValue(MissingValuesProperty);
        set => SetValue(MissingValuesProperty, value);
    }

    public bool InvertY
    {
        get => (bool)GetValue(InvertYProperty);
        set => SetValue(InvertYProperty, value);
    }

    public bool IsPaceScale
    {
        get => (bool)GetValue(IsPaceScaleProperty);
        set => SetValue(IsPaceScaleProperty, value);
    }

    public bool SmoothValues
    {
        get => (bool)GetValue(SmoothValuesProperty);
        set => SetValue(SmoothValuesProperty, value);
    }

    private sealed class AxisLineChartDrawable : IDrawable
    {
        private const int YTickCount = 4;
        private const int SmoothingWindow = 5;
        private const float LabelFontSize = 10f;
        private const float LeftGutter = 40f;
        private const float RightPadding = 8f;
        private const float TopPadding = 10f;
        private const float BottomGutter = 22f;

        private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

        private readonly AxisLineChartView owner;

        public AxisLineChartDrawable(AxisLineChartView owner)
        {
            this.owner = owner;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float plotWidth = dirtyRect.Width - LeftGutter - RightPadding;
            float plotHeight = dirtyRect.Height - TopPadding - BottomGutter;
            if (plotWidth <= 0 || plotHeight <= 0)
            {
                return;
            }

            if (owner.Series is { Count: > 0 } series)
            {
                DrawComparisonSeries(canvas, series, plotWidth, plotHeight);
                return;
            }

            if (owner.Values is not IList<double> values || values.Count < 2)
            {
                return;
            }

            var smoothed = owner.SmoothValues ? Smooth(values) : values.ToList();
            double min = smoothed.Min();
            double max = smoothed.Max();
            double range = Math.Max(max - min, 0.0001);

            canvas.FontSize = LabelFontSize;
            canvas.FontColor = owner.LabelColor;

            DrawYAxis(canvas, min, range, plotWidth, plotHeight);
            DrawXAxis(canvas, plotWidth, plotHeight);

            var normalized = Normalize(smoothed, min, range, owner.InvertY);
            var linePath = BuildPath(normalized, plotWidth, plotHeight);

            var fillPath = BuildPath(normalized, plotWidth, plotHeight);
            fillPath.LineTo(LeftGutter + plotWidth, TopPadding + plotHeight);
            fillPath.LineTo(LeftGutter, TopPadding + plotHeight);
            fillPath.Close();

            canvas.FillColor = owner.FillColor;
            canvas.FillPath(fillPath);

            canvas.StrokeColor = owner.LineColor;
            canvas.StrokeSize = 2.5f;
            canvas.DrawPath(linePath);
        }

        private void DrawComparisonSeries(
            ICanvas canvas,
            IReadOnlyList<ComparisonCurve> series,
            float plotWidth,
            float plotHeight)
        {
            var prepared = new List<List<double>>(series.Count);
            for (int index = 0; index < series.Count; index++)
            {
                var missing = MissingAt(index);
                var values = series[index].Values.ToList();
                prepared.Add(owner.SmoothValues ? Smooth(values, missing) : values);
            }

            var measured = new List<double>();
            for (int index = 0; index < prepared.Count; index++)
            {
                var missing = MissingAt(index);
                measured.AddRange(prepared[index].Where((_, valueIndex) => !IsMissing(missing, valueIndex)));
            }

            if (measured.Count < 2)
            {
                return;
            }

            double min = measured.Min();
            double max = measured.Max();
            double range = Math.Max(max - min, 0.0001);

            canvas.FontSize = LabelFontSize;
            canvas.FontColor = owner.LabelColor;
            DrawYAxis(canvas, min, range, plotWidth, plotHeight);
            DrawXAxis(canvas, plotWidth, plotHeight);

            for (int index = 0; index < prepared.Count; index++)
            {
                if (prepared[index].Count < 2)
                {
                    continue;
                }

                var missing = MissingAt(index);
                var normalized = Normalize(prepared[index], min, range, owner.InvertY);
                canvas.StrokeColor = Color.FromArgb(series[index].ColorHex);
                canvas.StrokeSize = 2.5f;
                canvas.DrawPath(BuildPath(normalized, plotWidth, plotHeight, missing));
            }
        }

        private IList<bool>? MissingAt(int index) =>
            owner.MissingValues is { } masks && index < masks.Count
                ? masks[index]
                : null;

        private void DrawYAxis(ICanvas canvas, double min, double range, float plotWidth, float plotHeight)
        {
            float axisX = LeftGutter - 4f;
            float bottom = TopPadding + plotHeight;

            canvas.StrokeColor = owner.GridColor;
            canvas.StrokeSize = 1f;

            for (int tick = 0; tick <= YTickCount; tick++)
            {
                float y = TopPadding + plotHeight * (1f - (float)tick / YTickCount);
                double value = owner.InvertY
                    ? min + range - range * tick / YTickCount
                    : min + range * tick / YTickCount;
                string label = owner.IsPaceScale
                    ? TripDisplay.FormatPaceSeconds(value)
                    : value.ToString(owner.YFormat, GermanCulture);
                if (tick == YTickCount && !string.IsNullOrEmpty(owner.YUnit))
                {
                    label += $" {owner.YUnit}";
                }

                if (tick > 0)
                {
                    canvas.DrawLine(LeftGutter, y, LeftGutter + plotWidth, y);
                }

                canvas.DrawString(label, 0, y - LabelFontSize / 2, axisX, LabelFontSize * 1.4f,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // Axis lines
            canvas.DrawLine(LeftGutter, TopPadding, LeftGutter, bottom);
            canvas.DrawLine(LeftGutter, bottom, LeftGutter + plotWidth, bottom);
        }

        private void DrawXAxis(ICanvas canvas, float plotWidth, float plotHeight)
        {
            if (owner.XLabels is not { Count: > 0 } labels)
            {
                return;
            }

            float labelY = TopPadding + plotHeight + 4f;
            var visible = VisibleLabelIndices(labels.Count);
            foreach (int i in visible)
            {
                if (string.IsNullOrEmpty(labels[i]))
                {
                    continue;
                }

                float x = labels.Count == 1
                    ? LeftGutter + plotWidth / 2
                    : LeftGutter + plotWidth * i / (labels.Count - 1);

                var alignment = i == 0
                    ? HorizontalAlignment.Left
                    : i == labels.Count - 1
                        ? HorizontalAlignment.Right
                        : HorizontalAlignment.Center;

                float boxX = alignment switch
                {
                    HorizontalAlignment.Left => x,
                    HorizontalAlignment.Right => x - 60f,
                    _ => x - 30f,
                };

                canvas.DrawString(labels[i], boxX, labelY, 60f, LabelFontSize * 1.4f,
                    alignment, VerticalAlignment.Top);
            }
        }

        private static IEnumerable<int> VisibleLabelIndices(int count)
        {
            const int maximumVisible = 6;
            if (count <= maximumVisible)
            {
                return Enumerable.Range(0, count);
            }

            return Enumerable.Range(0, maximumVisible)
                .Select(index => (int)Math.Round(index * (count - 1.0) / (maximumVisible - 1)))
                .Distinct();
        }

        private static PathF BuildPath(
            IReadOnlyList<float> normalized,
            float plotWidth,
            float plotHeight,
            IList<bool>? missing = null)
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

                float x = LeftGutter + plotWidth * i / (normalized.Count - 1);
                float y = TopPadding + plotHeight * (1f - normalized[i]);
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

        private static List<float> Normalize(
            IReadOnlyList<double> values,
            double min,
            double range,
            bool invert)
        {
            return values
                .Select(value =>
                {
                    float normalized = (float)((value - min) / range);
                    return invert ? 1f - normalized : normalized;
                })
                .ToList();
        }

        private static List<double> Smooth(IList<double> values, IList<bool>? missing = null)
        {
            var result = new List<double>(values.Count);
            int half = SmoothingWindow / 2;
            for (int i = 0; i < values.Count; i++)
            {
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
    }
}
