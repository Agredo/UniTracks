using System.Globalization;

namespace UniTracks.Maui.Views.Converter;

/// <summary>
/// True when both bound values are equal. Used by chip templates to highlight the item that matches
/// the view model's current selection.
/// </summary>
public class AreEqualMultiValueConverter : IMultiValueConverter
{
    public object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values is { Length: 2 } && Equals(values[0], values[1]);

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
