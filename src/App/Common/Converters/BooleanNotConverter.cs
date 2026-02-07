using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class BooleanNotConverter : IValueConverter
{
    public static readonly BooleanNotConverter Instance = new();

    private BooleanNotConverter() { }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}