using System.Collections;
using System.Collections.Specialized;
using UniTracks.Models.Stats;

namespace UniTracks.Maui.Views.Controls.Charts;

/// <summary>
/// Bar chart around a zero line, for differences instead of absolute values: a split faster than
/// the reference grows upwards, a slower one downwards. Values come as <see cref="ChartEntry"/>
/// and may be negative, which <see cref="BarChartView"/> cannot draw.
/// </summary>
public class DivergingBarChartView : GraphicsView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(DivergingBarChartView), null,
        propertyChanged: (b, _, newValue) => ((DivergingBarChartView)b).OnItemsSourceChanged(newValue));

    public static readonly BindableProperty FasterColorProperty = BindableProperty.Create(
        nameof(FasterColor), typeof(Color), typeof(DivergingBarChartView), Color.FromArgb("#4DE790"),
        propertyChanged: (b, _, _) => ((DivergingBarChartView)b).InvalidateChart());

    public static readonly BindableProperty SlowerColorProperty = BindableProperty.Create(
        nameof(SlowerColor), typeof(Color), typeof(DivergingBarChartView), Color.FromArgb("#FFB04D"),
        propertyChanged: (b, _, _) => ((DivergingBarChartView)b).InvalidateChart());

    public static readonly BindableProperty LabelColorProperty = BindableProperty.Create(
        nameof(LabelColor), typeof(Color), typeof(DivergingBarChartView), Color.FromArgb("#6B7A6D"),
        propertyChanged: (b, _, _) => ((DivergingBarChartView)b).InvalidateChart());

    public static readonly BindableProperty ValueColorProperty = BindableProperty.Create(
        nameof(ValueColor), typeof(Color), typeof(DivergingBarChartView), Color.FromArgb("#A9B8AC"),
        propertyChanged: (b, _, _) => ((DivergingBarChartView)b).InvalidateChart());

    public static readonly BindableProperty AxisColorProperty = BindableProperty.Create(
        nameof(AxisColor), typeof(Color), typeof(DivergingBarChartView), Color.FromArgb("#3A4A3D"),
        propertyChanged: (b, _, _) => ((DivergingBarChartView)b).InvalidateChart());

    private INotifyCollectionChanged? observedCollection;

    public DivergingBarChartView()
    {
        Drawable = new DivergingBarDrawable(this);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Colour of bars below the zero line, i.e. a split that was faster than the reference.</summary>
    public Color FasterColor
    {
        get => (Color)GetValue(FasterColorProperty);
        set => SetValue(FasterColorProperty, value);
    }

    public Color SlowerColor
    {
        get => (Color)GetValue(SlowerColorProperty);
        set => SetValue(SlowerColorProperty, value);
    }

    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    public Color ValueColor
    {
        get => (Color)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    public Color AxisColor
    {
        get => (Color)GetValue(AxisColorProperty);
        set => SetValue(AxisColorProperty, value);
    }

    private void OnItemsSourceChanged(object? newValue)
    {
        if (observedCollection is not null)
        {
            observedCollection.CollectionChanged -= OnSourceCollectionChanged;
            observedCollection = null;
        }

        if (newValue is INotifyCollectionChanged observable)
        {
            observedCollection = observable;
            observedCollection.CollectionChanged += OnSourceCollectionChanged;
        }

        InvalidateChart();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateChart();

    /// <summary><see cref="GraphicsView.Invalidate"/> is main-thread only.</summary>
    private void InvalidateChart()
    {
        if (MainThread.IsMainThread)
        {
            Invalidate();
        }
        else
        {
            Dispatcher.Dispatch(Invalidate);
        }
    }

    private sealed class DivergingBarDrawable : IDrawable
    {
        private const float LabelHeight = 18f;
        private const float ValueHeight = 15f;
        private const float MinBarHeight = 2f;

        private readonly DivergingBarChartView owner;

        public DivergingBarDrawable(DivergingBarChartView owner)
        {
            this.owner = owner;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var entries = owner.ItemsSource?.Cast<ChartEntry>().ToList();
            if (entries is null || entries.Count == 0)
            {
                return;
            }

            float chartTop = ValueHeight;
            float chartBottom = dirtyRect.Height - LabelHeight - ValueHeight;
            float chartHeight = Math.Max(1f, chartBottom - chartTop);
            float zeroLine = chartTop + chartHeight / 2f;
            float halfHeight = chartHeight / 2f;

            double maxValue = Math.Max(entries.Max(entry => Math.Abs(entry.Value)), 0.001);

            canvas.StrokeColor = owner.AxisColor;
            canvas.StrokeSize = 1f;
            canvas.DrawLine(dirtyRect.Left, zeroLine, dirtyRect.Right, zeroLine);

            float slotWidth = dirtyRect.Width / entries.Count;
            float barWidth = Math.Min(slotWidth * 0.6f, 42f);
            float cornerRadius = Math.Min(6f, barWidth / 3f);

            canvas.FontSize = 10f;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float center = slotWidth * i + slotWidth / 2f;
                float barHeight = Math.Max(MinBarHeight, (float)(Math.Abs(entry.Value) / maxValue) * halfHeight);

                bool faster = entry.Value < 0;
                canvas.FillColor = faster ? owner.FasterColor : owner.SlowerColor;

                RectF barRect;
                if (faster)
                {
                    barRect = new RectF(center - barWidth / 2f, zeroLine, barWidth, barHeight);
                    canvas.FillRoundedRectangle(barRect, 0, 0, cornerRadius, cornerRadius);
                }
                else
                {
                    barRect = new RectF(center - barWidth / 2f, zeroLine - barHeight, barWidth, barHeight);
                    canvas.FillRoundedRectangle(barRect, cornerRadius, cornerRadius, 0, 0);
                }

                canvas.FontColor = owner.ValueColor;
                string valueText = FormatValue(entry.Value);
                float valueTop = faster
                    ? zeroLine + barHeight
                    : zeroLine - barHeight - ValueHeight;

                canvas.DrawString(valueText, center - slotWidth / 2f, valueTop, slotWidth, ValueHeight,
                    HorizontalAlignment.Center, VerticalAlignment.Center);

                canvas.FontColor = owner.LabelColor;
                canvas.DrawString(entry.Label, center - slotWidth / 2f, dirtyRect.Height - LabelHeight,
                    slotWidth, LabelHeight, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

        /// <summary>Seconds, signed the way the bar grows: "+8" slower, "−4" faster.</summary>
        private static string FormatValue(double value)
        {
            double absolute = Math.Abs(value);
            string magnitude = absolute >= 10 ? absolute.ToString("0") : absolute.ToString("0.#");
            if (absolute < 1)
            {
                return "0";
            }

            return (value > 0 ? "+" : "−") + magnitude;
        }
    }
}
