using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace UniTracks.Maui.Views.Converter;

public class DateTimeDifferenceConverter : IMultiValueConverter
{
    public object? Convert(object?[]? values, Type targetType, [NotNull] object? parameter, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        //if (parameter is not string expression)
        //{
        //    throw new ArgumentException("The parameter should be of type DateTimeOffset.");
        //}

        if (values is null || values.Any(x => x is not DateTimeOffset) && values.Length == 2)
        {
            return default;
        }

        DateTimeOffset endDate = (DateTimeOffset)values[0];
        DateTimeOffset startDate = (DateTimeOffset)values[1];
       return (endDate - startDate);
    }

    /// <summary>
    /// This method is not supported and will throw a <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="value">N/A</param>
    /// <param name="targetTypes">N/A</param>
    /// <param name="parameter">N/A</param>
    /// <param name="culture">N/A</param>
    /// <returns>N/A</returns>
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo? culture)
        => throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}

