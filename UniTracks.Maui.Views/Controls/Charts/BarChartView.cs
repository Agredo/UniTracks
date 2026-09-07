using System.Collections;
using UniTracks.Models.Stats;

namespace UniTracks.Maui.Views.Controls.Charts;

/// <summary>
/// Reusable bar chart (e.g. weekly kilometres). Pure GraphicsView + IDrawable,
/// themed via bindable colors so it fits the app palette without new dependencies.
/// Bind <see cref="ItemsSource"/> to a collection of <see cref="ChartEntry"/>.
/// </summary>
public class BarChartView : GraphicsView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(BarChartView), null,
        propertyChanged: (b, _, _) => ((BarChartView)b).Invalidate());

    public static readonly BindableProperty BarColorProperty = BindableProperty.Create(
        nameof(BarColor), typeof(Color), typeof(BarChartView), Color.FromArgb("#2E7D54"),
        propertyChanged: (b, _, _) => ((BarChartView)b).Invalidate());

    public static readonly BindableProperty HighlightColorProperty = BindableProperty.Create(
        nameof(HighlightColor), typeof(Color), typeof(BarChartView), Color.FromArgb("#4DE790"),
        propertyChanged: (b, _, _) => ((BarChartView)b).Invalidate());

    public static readonly BindableProperty LabelColorProperty = BindableProperty.Create(
        nameof(LabelColor), typeof(Color), typeof(BarChartView), Color.FromArgb("#6B7A6D"),
        propertyChanged: (b, _, _) => ((BarChartView)b).Invalidate());

    public static readonly BindableProperty ValueColorProperty = BindableProperty.Create(
        nameof(ValueColor), typeof(Color), typeof(BarChartView), Color.FromArgb("#A9B8AC"),
        propertyChanged: (b, _, _) => ((BarChartView)b).Invalidate());

    public BarChartView()
    {
        Drawable = new BarChartDrawable(this);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
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

    private sealed class BarChartDrawable : IDrawable
    {
        private const float LabelHeight = 18f;
        private const float ValueHeight = 16f;
        private const float MinBarHeight = 2f;

        private readonly BarChartView owner;

        public BarChartDrawable(BarChartView owner)
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

            float chartBottom = dirtyRect.Height - LabelHeight;
            float chartTop = ValueHeight;
            float chartHeight = Math.Max(1f, chartBottom - chartTop);

            double maxValue = Math.Max(entries.Max(e => e.Value), 0.001);

            float slotWidth = dirtyRect.Width / entries.Count;
            float barWidth = Math.Min(slotWidth * 0.6f, 42f);
            float cornerRadius = Math.Min(6f, barWidth / 3f);

            canvas.FontSize = 10f;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float center = slotWidth * i + slotWidth / 2f;

                float barHeight = Math.Max(MinBarHeight, (float)(entry.Value / maxValue) * chartHeight);
                var barRect = new RectF(center - barWidth / 2f, chartBottom - barHeight, barWidth, barHeight);

                canvas.FillColor = entry.IsHighlighted ? owner.HighlightColor : owner.BarColor;
                canvas.FillRoundedRectangle(barRect, cornerRadius, cornerRadius, 0, 0);

                if (entry.Value > 0)
                {
                    canvas.FontColor = owner.ValueColor;
                    string valueText = entry.Value >= 10 ? entry.Value.ToString("0") : entry.Value.ToString("0.#");
                    canvas.DrawString(valueText, center - slotWidth / 2f, 0, slotWidth, ValueHeight,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                }

                canvas.FontColor = owner.LabelColor;
                canvas.DrawString(entry.Label, center - slotWidth / 2f, chartBottom, slotWidth, LabelHeight,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
}
