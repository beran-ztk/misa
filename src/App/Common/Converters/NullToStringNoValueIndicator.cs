using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Misa.Ui.Avalonia.Common.Converters;

public class NullToStringNoValueIndicator : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? "---" : value.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}