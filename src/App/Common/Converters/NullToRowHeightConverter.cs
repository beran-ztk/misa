using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Misa.Ui.Avalonia.Common.Converters;

public class NullToRowHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return new GridLength(0);

        if (value is string s && string.IsNullOrWhiteSpace(s))
            return new GridLength(0);

        return GridLength.Auto;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}