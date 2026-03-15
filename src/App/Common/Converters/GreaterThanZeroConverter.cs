using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class GreaterThanZeroConverter : IValueConverter
{
    public static readonly GreaterThanZeroConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i > 0,
            long l => l > 0,
            double d => d > 0,
            float f => f > 0,
            decimal m => m > 0,
            _ => false
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}