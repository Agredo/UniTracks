namespace UniTracks.Maui.Views.Controls;

/// <summary>
/// A small dot in the colour of one comparison member. The colour travels as a hex string because the
/// view models describe the comparison without knowing anything about MAUI types.
/// </summary>
public partial class ColorDot : ContentView
{
    public static readonly BindableProperty ColorHexProperty = BindableProperty.Create(
        nameof(ColorHex), typeof(string), typeof(ColorDot), null,
        propertyChanged: OnColorHexPropertyChanged);

    public ColorDot()
    {
        InitializeComponent();
    }

    public string? ColorHex
    {
        get => (string?)GetValue(ColorHexProperty);
        set => SetValue(ColorHexProperty, value);
    }

    private static void OnColorHexPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ColorDot dot)
        {
            dot.Dot.Fill = string.IsNullOrWhiteSpace(dot.ColorHex)
                ? new SolidColorBrush(Microsoft.Maui.Graphics.Colors.Transparent)
                : new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb(dot.ColorHex));
        }
    }
}
