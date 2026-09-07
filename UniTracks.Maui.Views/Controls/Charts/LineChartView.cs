using System.Collections;

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

    public LineChartView()
    {
        Drawable = new LineChartDrawable(this);
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

            var secondary = Normalize(owner.SecondaryValues);
            if (secondary is { Count: > 1 })
            {
                var secondaryPath = BuildPath(secondary, width, height);
                canvas.StrokeColor = owner.SecondaryColor;
                canvas.StrokeSize = 1.5f;
                canvas.StrokeDashPattern = new[] { 4f, 3f };
                canvas.DrawPath(secondaryPath);
                canvas.StrokeDashPattern = null;
            }

            var primary = Normalize(owner.PrimaryValues);
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

        private static List<float>? Normalize(IEnumerable? values)
        {
            if (values is not IList<double> list || list.Count == 0)
            {
                return null;
            }

            var smoothed = Smooth(list);
            double min = smoothed.Min();
            double max = smoothed.Max();
            double range = Math.Max(max - min, 0.0001);

            return smoothed.Select(v => (float)((v - min) / range)).ToList();
        }

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
