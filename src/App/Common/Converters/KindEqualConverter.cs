using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Misa.App.Common.Converters;

public sealed class KindEqualConverter : IValueConverter
{
    public static readonly KindEqualConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}